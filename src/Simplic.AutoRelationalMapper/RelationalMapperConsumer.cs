using Dapper;
using MassTransit;
using Simplic.Sql;
using System;
using System.Collections.Generic;
using System.Linq;
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

            ResolveObjects(obj, stack, queue);

            await sqlService.OpenConnection(async (c) =>
            {
                while (queue.Any())
                {
                    var currentObj = queue.Dequeue();

                    // Cache last object for other foreign-key references
                    lastObjects[currentObj.GetType()] = currentObj;

                    var configuration = configurations.FirstOrDefault(x => x.Type == currentObj.GetType());

                    if (currentObj is IDictionary<string, object> addon)
                    {
                        lastObjects.TryGetValue(configuration.Owner, out object owner);

                        if (owner != null)
                        {
                            // Set owner foreign-key-value

                            foreach (var foreignKey in configuration.ForeignKeys)
                            {

                            }
                        }
                    }
                    else
                    {
                        var columns = sqlColumnService.GetModelDBColumnNames(configuration.Table, configuration.Type, null);
                        var statement = $"INSERT INTO {configuration.Table} ({{0}}) ON EXISTING UPDATE VALUES ({{1}})";

                        // Write to database
                        await c.ExecuteAsync(statement, currentObj);
                    }
                }
            });
        }

        private void ResolveObjects(object obj, Stack<object> stack, Queue<object> queue, IList<object> checkedObjects = null)
        {
            if (obj == null)
                return;

            // Create object cache if not done yet
            checkedObjects = checkedObjects ?? new List<object>();

            if (checkedObjects.Contains(obj))
                return;

            // Add to already checked objects to prevent stackoverflow exception
            checkedObjects.Add(obj);

            var configuration = configurations.FirstOrDefault(x => x.Type == obj.GetType());

            if (configuration == null)
                return;

            if (!stack.Contains(obj))
                stack.Push(obj);

            if (!queue.Contains(obj))
                queue.Enqueue(obj);

            // Check for possible child stacks
            var properties = obj.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                               .Where(x => x.MemberType == System.Reflection.MemberTypes.Property)

                               // Check whether the type is part of the configurations. If an owner is set, the type must match too
                               .Where(x => configurations.Any(y => x.DeclaringType == y.Type && (y.Owner == null || obj.GetType() == y.Owner)));

            foreach (var property in properties)
            {
                // Build recursive tree.
                ResolveObjects(property.GetValue(obj), stack, queue, checkedObjects);
            }
        }

        protected abstract T GetObject(E @event);

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
