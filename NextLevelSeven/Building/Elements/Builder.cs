﻿using System.Collections.Generic;
using System.Linq;
using NextLevelSeven.Core;
using NextLevelSeven.Core.Codec;
using NextLevelSeven.Core.Encoding;
using NextLevelSeven.Utility;

namespace NextLevelSeven.Building.Elements
{
    /// <summary>Base class for message builders.</summary>
    internal abstract class Builder : IElementBuilder
    {
        /// <summary>Initialize the message builder base class.</summary>
        internal Builder()
        {
            Encoding = new BuilderEncodingConfiguration(this);
        }

        /// <summary>Initialize the message builder base class.</summary>
        /// <param name="config">Message's encoding configuration.</param>
        /// <param name="index">Index in the parent.</param>
        internal Builder(IEncoding config, int index)
        {
            Encoding = config;
            Index = index;
        }

        /// <summary>Get or set the character used to separate component-level content.</summary>
        public virtual char ComponentDelimiter { get; set; }

        /// <summary>Get or set the character used to signify escape sequences.</summary>
        public virtual char EscapeCharacter { get; set; }

        /// <summary>Get or set the character used to separate fields.</summary>
        public virtual char FieldDelimiter { get; set; }

        /// <summary>Get or set the character used to separate field repetition content.</summary>
        public virtual char RepetitionDelimiter { get; set; }

        /// <summary>Get or set the character used to separate subcomponent-level content.</summary>
        public virtual char SubcomponentDelimiter { get; set; }

        /// <summary>Get the encoding used by this builder.</summary>
        public IEncoding Encoding { get; private set; }

        /// <summary>Get the index at which this builder is located in its descendant.</summary>
        public int Index { get; private set; }

        /// <summary>Deep clone this element.</summary>
        /// <returns>Cloned element.</returns>
        public abstract IElement Clone();

        /// <summary>Get or set this element's value.</summary>
        public abstract string Value { get; set; }

        /// <summary>Get or set this element's sub-values.</summary>
        public abstract IEnumerable<string> Values { get; set; }

        /// <summary>Get a converter which will interpret this element's value as other types.</summary>
        public virtual IEncodedTypeConverter Codec
        {
            get
            {
                return new EncodedTypeConverter(this);
            }
        }

        /// <summary>Get the number of sub-values in this element.</summary>
        public abstract int ValueCount { get; }

        /// <summary>Get this element's section delimiter.</summary>
        public abstract char Delimiter { get; }

        /// <summary>Get the descendant builder at the specified index.</summary>
        /// <param name="index">Index to reference.</param>
        /// <returns>Descendant builder.</returns>
        public IElement this[int index]
        {
            get { return GetGenericElement(index); }
        }

        /// <summary>Get the ancestor element. Null if it's a root element.</summary>
        IElement IElement.Ancestor
        {
            get { return GetAncestor(); }
        }

        /// <summary>Get descendant elements. For subcomponents, this will be empty.</summary>
        IEnumerable<IElement> IElement.Descendants
        {
            get { return GetDescendants(); }
        }

        /// <summary>Unique key of the element within the message.</summary>
        public string Key
        {
            get { return ElementOperations.GetKey(this); }
        }

        /// <summary>Get the next available index.</summary>
        public virtual int NextIndex
        {
            get { return GetDescendants().Where(d => d.Exists).Max(d => d.Index) + 1; }
        }

        /// <summary>Erase this element's content and mark it non-existant.</summary>
        public void Erase()
        {
            Value = null;
        }

        /// <summary>True, if the element is considered to exist.</summary>
        public abstract bool Exists { get; }

        /// <summary>Get the encoding used by this builder.</summary>
        IReadOnlyEncoding IElement.Encoding
        {
            get { return Encoding; }
        }

        /// <summary>Get the encoding used by this builder.</summary>
        IEncoding IElementBuilder.Encoding
        {
            get { return Encoding; }
        }

        /// <summary>Delete a descendant element.</summary>
        /// <param name="index">Index to delete at.</param>
        public abstract void Delete(int index);

        /// <summary>Insert a copy of the specified element at the specified descendant index.</summary>
        /// <param name="index">Index to insert into.</param>
        /// <param name="element">Element to insert.</param>
        public abstract IElement Insert(int index, IElement element);

        /// <summary>Insert a copy of the specified element at the specified descendant index.</summary>
        /// <param name="index">Index to insert into.</param>
        /// <param name="value">Value to insert.</param>
        public abstract IElement Insert(int index, string value);

        /// <summary>Move a descendant element to another index.</summary>
        /// <param name="sourceIndex">Index to move from.</param>
        /// <param name="targetIndex">Index to move to.</param>
        public abstract void Move(int sourceIndex, int targetIndex);

        /// <summary>Get the element at the specified index as an IElement.</summary>
        /// <param name="index">Index at which to get the element.</param>
        /// <returns>Generic element.</returns>
        protected abstract IElement GetGenericElement(int index);

        /// <summary>Get this builder's contents as a string.</summary>
        /// <returns>Builder's contents.</returns>
        public override sealed string ToString()
        {
            return Value ?? string.Empty;
        }

        /// <summary>Get the ancestor element.</summary>
        /// <returns>Ancestor element.</returns>
        protected virtual IElement GetAncestor()
        {
            return null;
        }

        /// <summary>Get descendant elements.</summary>
        /// <returns>Descendant elements.</returns>
        protected virtual IEnumerable<IElement> GetDescendants()
        {
            var count = ValueCount;
            for (var i = 1; i <= count; i++)
            {
                yield return this[i];
            }
        }

        /// <summary>Delete a descendant element at the specified index.</summary>
        /// <typeparam name="TDescendant">Type of descendant element.</typeparam>
        /// <param name="cache">Cache to delete within.</param>
        /// <param name="index">Descendant index to delete.</param>
        protected static void DeleteDescendant<TDescendant>(IIndexedCache<TDescendant> cache, int index)
            where TDescendant : Builder
        {
            var values = cache.Where(c => c.Key != index).ToList();
            foreach (var value in values.Where(v => v.Key > index))
            {
                value.Value.Index--;
            }
            cache.Clear();
            foreach (var value in values)
            {
                cache[value.Value.Index] = value.Value;
            }
        }

        /// <summary>Move indices forward in preparation for insert.</summary>
        /// <typeparam name="TDescendant">Type of descendant element.</typeparam>
        /// <param name="cache">Cache to modify.</param>
        /// <param name="index">Descendant index.</param>
        private static void ShiftForInsert<TDescendant>(IIndexedCache<TDescendant> cache, int index)
            where TDescendant : Builder
        {
            var values = cache.ToList();
            foreach (var value in values.Where(v => v.Key >= index))
            {
                value.Value.Index++;
            }
            cache.Clear();
            foreach (var value in values)
            {
                cache[value.Value.Index] = value.Value;
            }
        }

        /// <summary>Insert a descendant element at the specified index.</summary>
        /// <typeparam name="TDescendant">Type of descendant element.</typeparam>
        /// <param name="cache">Cache to delete within.</param>
        /// <param name="index">Descendant index to delete.</param>
        /// <param name="value">Value to insert.</param>
        protected static IElementBuilder InsertDescendant<TDescendant>(IIndexedCache<TDescendant> cache, int index,
            string value) where TDescendant : Builder
        {
            ShiftForInsert(cache, index);
            cache[index].Value = value;
            return cache[index];
        }

        /// <summary>Insert a descendant element string at the specified index.</summary>
        /// <typeparam name="TDescendant">Type of descendant element.</typeparam>
        /// <param name="cache">Cache to delete within.</param>
        /// <param name="index">Descendant index to delete.</param>
        /// <param name="element">Element to insert.</param>
        protected static IElementBuilder InsertDescendant<TDescendant>(IIndexedCache<TDescendant> cache, int index,
            IElement element) where TDescendant : Builder
        {
            ShiftForInsert(cache, index);
            element.CopyTo(cache[index]);
            return cache[index];
        }

        /// <summary>Move a descendant element to another index within the cache provided.</summary>
        /// <typeparam name="TDescendant">Type of descendant element.</typeparam>
        /// <param name="cache">Cache to move within.</param>
        /// <param name="source">Source index.</param>
        /// <param name="target">Target index.</param>
        protected static void MoveDescendant<TDescendant>(IIndexedCache<TDescendant> cache, int source, int target)
            where TDescendant : Builder
        {
            var values = cache.ToList();
            var sourceValue = cache[source];

            foreach (var value in values.Where(v => v.Key > source))
            {
                value.Value.Index--;
            }
            foreach (var value in values.Where(v => v.Key > target))
            {
                value.Value.Index++;
            }

            sourceValue.Index = target;
            cache.Clear();
            foreach (var value in values)
            {
                cache[value.Value.Index] = value.Value;
            }
        }

        /// <summary>
        ///     Get the message for this builder.
        /// </summary>
        public IMessage Message
        {
            get
            {
                var ancestor = GetAncestor();
                while (ancestor != null)
                {
                    if (ancestor is IMessage)
                    {
                        return ancestor as IMessage;
                    }
                    ancestor = ancestor.Ancestor;
                }
                return null;
            }
        }
    }
}