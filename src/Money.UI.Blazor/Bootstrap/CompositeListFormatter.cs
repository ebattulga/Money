﻿using Neptuo;
using Neptuo.Activators;
using Neptuo.Formatters;
using Neptuo.Formatters.Metadata;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Money.Bootstrap
{
    public class CompositeListFormatter : IFormatter
    {
        private readonly Formatter inner;

        public CompositeListFormatter(ICompositeTypeProvider provider, IFactory<ICompositeStorage> storageFactory)
        {
            inner = new Formatter(provider, storageFactory);
        }

        public bool TryDeserialize(Stream input, IDeserializerContext context)
        {
            if (typeof(IList).IsAssignableFrom(context.OutputType))
            {
                DefaultDeserializerContext innerContext = new DefaultDeserializerContext(context.OutputType.GetGenericArguments()[0]);
                if (inner.TryDeserialize(input, innerContext))
                {
                    context.Output = innerContext.Output;
                    return true;
                }

                return false;
            }

            return inner.TryDeserialize(input, context);
        }

        public Task<bool> TryDeserializeAsync(Stream input, IDeserializerContext context)
        {
            return inner.TryDeserializeAsync(input, context);
        }

        public bool TrySerialize(object input, ISerializerContext context)
        {
            if (input is IList)
            {
                DefaultSerializerContext innerContext = new DefaultSerializerContext(context.InputType.GetGenericArguments()[0], context.Output);
                return inner.TrySerialize(input, innerContext);
            }

            return inner.TrySerialize(input, context);
        }

        public Task<bool> TrySerializeAsync(object input, ISerializerContext context)
        {
            return Task.FromResult(TrySerialize(input, context));
        }

        private class Formatter : CompositeTypeFormatter
        {
            public Formatter(ICompositeTypeProvider provider, IFactory<ICompositeStorage> storageFactory)
                : base(provider, storageFactory)
            {
            }

            protected override bool TryStore(object input, ISerializerContext context, CompositeType type, CompositeVersion typeVersion, ICompositeStorage storage)
            {
                if (input is IList list)
                {
                    storage.Add("Count", list.Count);
                    for (int i = 0; i < list.Count; i++)
                    {
                        object item = list[i];

                        ICompositeStorage innerStorage = storage.Add(i.ToString());
                        if (!base.TryStore(item, context, type, typeVersion, innerStorage))
                            return false;
                    }

                    return true;
                }

                return base.TryStore(input, context, type, typeVersion, storage);
            }

            protected override bool TryLoad(Stream input, IDeserializerContext context, CompositeType type, ICompositeStorage storage)
            {
                if (storage.TryGet("Count", out int count))
                {
                    Console.WriteLine($"CLF: Count {count}.");

                    Type listType = typeof(List<>).MakeGenericType(type.Type);
                    IList list = (IList)Activator.CreateInstance(listType);

                    Console.WriteLine($"CLF: List: {list != null}");

                    for (int i = 0; i < count; i++)
                    {
                        if (storage.TryGet(i.ToString(), out ICompositeStorage innerStorage) && base.TryLoad(input, context, type, innerStorage))
                        {
                            Console.WriteLine($"CLF: Add item {context.Output != null}.");
                            list.Add(context.Output);
                        }
                        else
                        {
                            Console.WriteLine($"CLF: Item failed at index {i}.");
                            return false;
                        }
                    }

                    context.Output = list;
                    return true;
                }

                Console.WriteLine($"CLF: Count not found.");
                return base.TryLoad(input, context, type, storage);
            }
        }
    }
}