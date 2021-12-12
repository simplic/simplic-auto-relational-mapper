using Dapper;
using MassTransit;
using Simplic.Sql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simplic.AutoRelationalMapper
{
    /// <summary>
    /// Base class that can be used to store event/command information in a relational database
    /// </summary>
    /// <typeparam name="E">Event/command type</typeparam>
    /// <typeparam name="T">Root-object type for storing in the database</typeparam>
    public abstract class RelationalMapperConsumer<E, T> : IConsumer<E> where E : class where T : class
    {
        private readonly ISqlService sqlService;
        private readonly ISqlColumnService sqlColumnService;
        private readonly IList<ITableConfiguration> configurations = new List<ITableConfiguration>();

        /// <summary>
        /// Initialize consumer
        /// </summary>
        /// <param name="sqlService">Sql service</param>
        /// <param name="sqlColumnService">Sql column service</param>
        public RelationalMapperConsumer(ISqlService sqlService, ISqlColumnService sqlColumnService)
        {
            this.sqlService = sqlService;
            this.sqlColumnService = sqlColumnService;
        }

        /// <summary>
        /// Consume command and store information in the database
        /// </summary>
        /// <param name="context">Context instance, containing the data-message</param>
        public async Task Consume(ConsumeContext<E> context)
        {
            var obj = GetObject(context.Message);
            var lastObjects = new Dictionary<Type, object>();
            var stack = new Stack<object>();
            var queue = new Queue<object>();

            ParseObjects(obj, stack, queue);

            await sqlService.OpenConnection(async (c) =>
            {
                while (queue.Any())
                {
                    var currentObj = queue.Dequeue();

                    // Cache last object for other foreign-key references
                    lastObjects[currentObj.GetType()] = currentObj;

                    var configuration = configurations.FirstOrDefault(x => x.Type == currentObj.GetType());

                    var parameter = new DynamicParameters();
                    var columnNames = new StringBuilder();
                    var parameterNames = new StringBuilder();

                    var statement = $"INSERT INTO {configuration.Table} ({{0}}) ON EXISTING UPDATE VALUES ({{1}})";

                    void addColumn(string _columnName, object _value)
                    {
                        parameter.Add(_columnName, _value);

                        if (columnNames.Length != 0)
                            columnNames.Append(", ");

                        if (parameterNames.Length != 0)
                            parameterNames.Append(", ");

                        columnNames.Append(_columnName);
                        parameterNames.Append($":{_columnName}");
                    };

                    if (currentObj is IDictionary<string, object> addon)
                    {
                        var columns = sqlColumnService.GetColumns(configuration.Table, "default");

                        foreach (var kvp in addon)
                        {
                            var columnName = kvp.Key;

                            // Try get column name mapping
                            if (configuration.ColumnMapping.ContainsKey(columnName))
                                columnName = configuration.ColumnMapping[columnName];

                            if (columns.Any(x => x.Key.ToLower() == columnName.ToLower()))
                            {
                                addColumn(columnName, kvp.Value);
                            }
                        }
                    }
                    else
                    {
                        var columns = sqlColumnService.GetModelDBColumnNames(configuration.Table, configuration.Type, configuration.ColumnMapping);

                        foreach (var column in columns)
                        {
                            var value = GetValue(obj, column.Key);

                            addColumn(column.Value, column.Value);
                        }
                    }

                    foreach (var foreignKey in configuration.ForeignKeys)
                    {
                        // Try to find the last object (parent) that is the parent of the actual object
                        if (lastObjects.TryGetValue(foreignKey.Source, out object parent))
                        {
                            // Read value from parent and find correct column name
                            var columnName = foreignKey.ForeignKeyName;
                            var value = GetValue(parent, foreignKey.PrimaryKeyName);

                            // Try get column name mapping
                            if (configuration.ColumnMapping.ContainsKey(columnName))
                                columnName = configuration.ColumnMapping[columnName];

                            addColumn(columnName, value);
                        }
                        else
                        { 
                            // TODO: What should happend here?
                        }
                    }

                    statement = string.Format(statement, columnNames, parameterNames);

                    // Write to database
                    await c.ExecuteAsync(statement, parameter);
                }
            });
        }

        /// <summary>
        /// Get a property name
        /// </summary>
        /// <param name="obj">Object instance</param>
        /// <param name="propertyName">Property name</param>
        /// <returns>Value as object</returns>
        private object GetValue(object obj, string propertyName)
        {
            var type = obj.GetType();
            var property = type.GetProperties().FirstOrDefault(x => x.Name.ToLower() == propertyName.ToLower());

            if (property == null)
            {
                return property.GetValue(obj);
            }

            return null;
        }

        /// <summary>
        /// Parses objects recursivly and add to stack/queue for writing to database
        /// </summary>
        /// <param name="obj">Object to parse</param>
        /// <param name="stack">Stack to push objects to (reverse-order)</param>
        /// <param name="queue">Queue to enqueue objects to</param>
        internal void ParseObjects(object obj, Stack<object> stack, Queue<object> queue) => ParseObjects(obj, stack, queue, new List<string>());

        /// <summary>
        /// Parses objects recursivly and add to stack/queue for writing to database
        /// </summary>
        /// <param name="obj">Object to parse</param>
        /// <param name="stack">Stack to push objects to (reverse-order)</param>
        /// <param name="queue">Queue to enqueue objects to</param>
        /// <param name="checkedObjects">Already added objects</param>
        private void ParseObjects(object obj, Stack<object> stack, Queue<object> queue, IList<string> checkedObjects)
        {
            if (obj == null)
                return;

            // Create object cache if not done yet
            checkedObjects = checkedObjects ?? new List<string>();

            var configuration = configurations.FirstOrDefault(x => x.Type == obj.GetType());

            if (configuration == null)
                return;

            var key = $"{configuration.Table}_{string.Join("_", GetValues(obj, configuration.PrimaryKeys.OrderBy(x => x).ToList()).Select(x => x.Value?.ToString() ?? "<null>"))}";

            if (checkedObjects.Contains(key))
                return;

            // Add to already checked objects to prevent stackoverflow exception
            checkedObjects.Add(key);

            stack.Push(obj);
            queue.Enqueue(obj);

            // Check for possible child stacks
            var properties = obj.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                               .Where(x => x.MemberType == System.Reflection.MemberTypes.Property);

            foreach (var property in properties)
            {
                var type = property.PropertyType;
                var value = property.GetValue(obj);

                // Check whether the type is part of the configurations. If an owner is set, the type must match too
                if (configurations.Any(x => x.Type == type && (x.Owner == null || obj.GetType() == x.Owner)))
                {
                    // Build recursive tree.
                    ParseObjects(property.GetValue(obj), stack, queue, checkedObjects);
                }
                else if (value != null && value is System.Collections.IEnumerable enumerable)
                {
                    foreach (object item in enumerable)
                    {
                        if (item == null)
                            continue;

                        var itemType = item.GetType();

                        if (configurations.Any(x => x.Type == itemType && (x.Owner == null || obj.GetType() == x.Owner)))
                        {
                            // Build recursive tree.
                            ParseObjects(item, stack, queue, checkedObjects);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get property values as dictionary from an object
        /// </summary>
        /// <param name="obj">Object to read properties from</param>
        /// <param name="properties">List of properties to read</param>
        /// <returns>Dictionary containing property values as key-value (key = property name / value = property-value)</returns>
        private IDictionary<string, object> GetValues(object obj, IList<string> properties)
        {
            var values = new Dictionary<string, object>();

            foreach (var property in properties)
            {
                var propertyInfo = obj.GetType().GetProperties().FirstOrDefault(x => x.Name == property);
                if (propertyInfo == null)
                    throw new Exception($"Could not find property `{property}` in `{obj.GetType()}`");

                values[property] = propertyInfo.GetValue(obj);
            }

            return values;
        }

        /// <summary>
        /// Gets the root-object of the command
        /// </summary>
        /// <param name="command">Command instance</param>
        /// <returns>Object to write to database</returns>
        protected abstract T GetObject(E command);

        /// <summary>
        /// Create new table configuration
        /// </summary>
        /// <typeparam name="TTableObject">Object to map</typeparam>
        /// <param name="table">Target table name</param>
        /// <returns>Table configuration instance</returns>
        protected virtual TableConfiguration<TTableObject> MapTable<TTableObject>(string table) where TTableObject : class
        {
            var configuration = new TableConfiguration<TTableObject>
            {
                Table = table
            };

            configurations.Add(configuration);

            return configuration;
        }

        /// <summary>
        /// Create new table configuration, with a specific table-parent
        /// </summary>
        /// <typeparam name="TTableObject">Object to map</typeparam>
        /// <typeparam name="TObjectOwner">Table object owner (parent)</typeparam>
        /// <param name="table">Target table name</param>
        /// <returns>Table configuration instance</returns>
        protected virtual TableConfiguration<TTableObject> MapTable<TTableObject, TObjectOwner>(string table) where TTableObject : class
        {
            var configuration = new TableConfiguration<TTableObject>
            {
                Table = table,
                Owner = typeof(TObjectOwner)
            };

            configurations.Add(configuration);

            return configuration;
        }
    }
}
