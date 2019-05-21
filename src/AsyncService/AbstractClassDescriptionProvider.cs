using System;
using System.ComponentModel;

namespace AsyncService
{
    /// <summary>
    /// Provides a <see cref="TypeDescriptionProvider"/> used to instantiate an abstract class based on a non abstract type.
    /// </summary>
    /// <typeparam name="TAbstract">The abstract <see cref="Type"/> type to be instantiated.</typeparam>
    /// <typeparam name="TBase">The actual <see cref="Type"/> type to instantiate</typeparam>
    public class AbstractTypeDescriptionProvider<TAbstract, TBase> : TypeDescriptionProvider
    {
        /// <summary>
        /// Initializes a new <see cref="AbstractTypeDescriptionProvider{TAbstract, TBase}"/> instance.
        /// </summary>
        public AbstractTypeDescriptionProvider()
            : base(TypeDescriptor.GetProvider(typeof(TAbstract)))
        {
        }

        public override Type GetReflectionType(Type objectType, object instance)
        {
            if (objectType == typeof(TAbstract))
                return typeof(TBase);

            return base.GetReflectionType(objectType, instance);
        }

        public override object CreateInstance(IServiceProvider provider, Type objectType, Type[] argTypes, object[] args)
        {
            if (objectType == typeof(TAbstract))
                objectType = typeof(TBase);

            return base.CreateInstance(provider, objectType, argTypes, args);
        }
    }
}