﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using NextLevelSeven.Core;
using NextLevelSeven.Core.Codec;
using NextLevelSeven.Diagnostics;
using NextLevelSeven.Utility;

namespace NextLevelSeven.Building.Elements
{
    /// <summary>
    ///     Represents an HL7 segment.
    /// </summary>
    internal sealed class SegmentBuilder : BuilderBaseDescendant, ISegmentBuilder
    {
        /// <summary>
        ///     Descendant builders.
        /// </summary>
        private readonly Dictionary<int, FieldBuilder> _fieldBuilders = new Dictionary<int, FieldBuilder>();

        /// <summary>
        ///     Create a segment builder with the specified encoding configuration.
        /// </summary>
        /// <param name="builder">Ancestor builder.</param>
        /// <param name="index">Index in the ancestor.</param>
        /// <param name="value">Default value.</param>
        internal SegmentBuilder(BuilderBase builder, int index, string value = null)
            : base(builder, index)
        {
            if (value != null)
            {
                Value = value;
            }
        }

        /// <summary>
        ///     If true, this is an MSH segment which has special behavior in fields 1 and 2.
        /// </summary>
        public bool IsMsh
        {
            get
            {
                if (!_fieldBuilders.ContainsKey(0))
                {
                    return false;
                }
                return _fieldBuilders[0].Value == "MSH";
            }
        }

        /// <summary>
        ///     Get a descendant field builder.
        /// </summary>
        /// <param name="index">Index within the segment to get the builder from.</param>
        /// <returns>Field builder for the specified index.</returns>
        public new IFieldBuilder this[int index]
        {
            get
            {
                if (_fieldBuilders.ContainsKey(index))
                {
                    return _fieldBuilders[index];
                }

                // segment type is treated specially
                if (index == 0)
                {
                    _fieldBuilders[0] = new TypeFieldBuilder(this, OnTypeFieldModified, index);
                    return _fieldBuilders[0];
                }

                // msh-1 and msh-2 are treated specially
                if (IsMsh)
                {
                    if (index == 1)
                    {
                        _fieldBuilders[index] = new DelimiterFieldBuilder(this, index);
                        return _fieldBuilders[index];
                    }
                    if (index == 2)
                    {
                        _fieldBuilders[index] = new EncodingFieldBuilder(this, index);
                        return _fieldBuilders[index];
                    }
                }

                _fieldBuilders[index] = new FieldBuilder(this, index);
                return _fieldBuilders[index];
            }
        }

        /// <summary>
        ///     Get the number of fields in this segment, including fields with no content.
        /// </summary>
        public override int ValueCount
        {
            get { return (_fieldBuilders.Count > 0) ? _fieldBuilders.Max(kv => kv.Key) + 1 : 0; }
        }

        /// <summary>
        ///     Get or set the three-letter type field of this segment.
        /// </summary>
        public string Type
        {
            get { return this[0].Value; }
            set { Field(0, value); }
        }

        /// <summary>
        ///     Get or set field content within this segment.
        /// </summary>
        public override IEnumerable<string> Values
        {
            get
            {
                return new WrapperEnumerable<string>(index => this[index].Value,
                    (index, data) => Field(index, data),
                    () => ValueCount);
            }
            set { Fields(value.ToArray()); }
        }

        /// <summary>
        ///     Get or set the segment string.
        /// </summary>
        public override string Value
        {
            get
            {
                var index = 0;
                var result = new StringBuilder();

                if (_fieldBuilders.Count <= 0)
                {
                    return string.Empty;
                }

                var typeIsMsh = (_fieldBuilders.ContainsKey(0) && _fieldBuilders[0].Value == "MSH");

                foreach (var field in _fieldBuilders.OrderBy(i => i.Key))
                {
                    if (field.Key < 0)
                    {
                        continue;
                    }

                    while (index < field.Key)
                    {
                        result.Append(FieldDelimiter);
                        index++;
                    }

                    if (typeIsMsh && index == 1)
                    {
                        index++;
                    }
                    else
                    {
                        result.Append(field.Value);
                    }
                }

                return result.ToString();
            }
            set { Segment(value); }
        }

        /// <summary>
        ///     Set a component's content.
        /// </summary>
        /// <param name="fieldIndex">Field index.</param>
        /// <param name="repetition">Field repetition index.</param>
        /// <param name="componentIndex">Component index.</param>
        /// <param name="value">New value.</param>
        /// <returns>This SegmentBuilder, for chaining purposes.</returns>
        public ISegmentBuilder Component(int fieldIndex, int repetition, int componentIndex, string value)
        {
            this[fieldIndex].Component(repetition, componentIndex, value);
            return this;
        }

        /// <summary>
        ///     Replace all component values within a field repetition.
        /// </summary>
        /// <param name="fieldIndex">Field index.</param>
        /// <param name="repetition">Field repetition index.</param>
        /// <param name="components">Values to replace with.</param>
        /// <returns>This SegmentBuilder, for chaining purposes.</returns>
        public ISegmentBuilder Components(int fieldIndex, int repetition, params string[] components)
        {
            this[fieldIndex].Components(repetition, components);
            return this;
        }

        /// <summary>
        ///     Set a sequence of components within a field repetition, beginning at the specified start index.
        /// </summary>
        /// <param name="fieldIndex">Field index.</param>
        /// <param name="repetition">Field repetition index.</param>
        /// <param name="startIndex">Component index to begin replacing at.</param>
        /// <param name="components">Values to replace with.</param>
        /// <returns>This SegmentBuilder, for chaining purposes.</returns>
        public ISegmentBuilder Components(int fieldIndex, int repetition, int startIndex, params string[] components)
        {
            this[fieldIndex].Components(repetition, startIndex, components);
            return this;
        }

        /// <summary>
        ///     Set a field's content.
        /// </summary>
        /// <param name="fieldIndex">Field index.</param>
        /// <param name="value">New value.</param>
        /// <returns>This SegmentBuilder, for chaining purposes.</returns>
        public ISegmentBuilder Field(int fieldIndex, string value)
        {
            if (fieldIndex > 0 && _fieldBuilders.ContainsKey(fieldIndex))
            {
                _fieldBuilders.Remove(fieldIndex);
            }
            this[fieldIndex].Field(value);
            return this;
        }

        /// <summary>
        ///     Replace all field values within this segment.
        /// </summary>
        /// <param name="fields">Values to replace with.</param>
        /// <returns>This SegmentBuilder, for chaining purposes.</returns>
        public ISegmentBuilder Fields(params string[] fields)
        {
            _fieldBuilders.Clear();
            return Fields(0, fields);
        }

        /// <summary>
        ///     Set a sequence of fields within this segment, beginning at the specified start index.
        /// </summary>
        /// <param name="startIndex">Field index to begin replacing at.</param>
        /// <param name="fields">Values to replace with.</param>
        /// <returns>This SegmentBuilder, for chaining purposes.</returns>
        public ISegmentBuilder Fields(int startIndex, params string[] fields)
        {
            foreach (var field in fields)
            {
                Field(startIndex++, field);
            }

            return this;
        }

        /// <summary>
        ///     Set a field repetition's content.
        /// </summary>
        /// <param name="fieldIndex">Field index.</param>
        /// <param name="repetition">Field repetition index.</param>
        /// <param name="value">New value.</param>
        /// <returns>This SegmentBuilder, for chaining purposes.</returns>
        public ISegmentBuilder FieldRepetition(int fieldIndex, int repetition, string value)
        {
            this[fieldIndex].FieldRepetition(repetition, value);
            return this;
        }

        /// <summary>
        ///     Replace all field repetitions within a field.
        /// </summary>
        /// <param name="fieldIndex">Field index.</param>
        /// <param name="repetitions">Values to replace with.</param>
        /// <returns>This SegmentBuilder, for chaining purposes.</returns>
        public ISegmentBuilder FieldRepetitions(int fieldIndex, params string[] repetitions)
        {
            this[fieldIndex].FieldRepetitions(repetitions);
            return this;
        }

        /// <summary>
        ///     Set a sequence of field repetitions within a field, beginning at the specified start index.
        /// </summary>
        /// <param name="fieldIndex">Field index.</param>
        /// <param name="startIndex">Field repetition index to begin replacing at.</param>
        /// <param name="repetitions">Values to replace with.</param>
        /// <returns>This SegmentBuilder, for chaining purposes.</returns>
        public ISegmentBuilder FieldRepetitions(int fieldIndex, int startIndex, params string[] repetitions)
        {
            this[fieldIndex].FieldRepetitions(startIndex, repetitions);
            return this;
        }

        /// <summary>
        ///     Set this segment's content.
        /// </summary>
        /// <param name="value">New value.</param>
        /// <returns>This SegmentBuilder, for chaining purposes.</returns>
        public ISegmentBuilder Segment(string value)
        {
            if (value.Length > 3)
            {
                var isMsh = value.Substring(0, 3) == "MSH";

                if (isMsh)
                {
                    FieldDelimiter = value[3];
                }

                var values = value.Split(FieldDelimiter);
                if (isMsh)
                {
                    var valueList = values.ToList();
                    valueList.Insert(1, new string(FieldDelimiter, 1));
                    values = valueList.ToArray();
                }
                return Fields(values.ToArray());
            }

            return this;
        }

        /// <summary>
        ///     Set a subcomponent's content.
        /// </summary>
        /// <param name="fieldIndex">Field index.</param>
        /// <param name="repetition">Field repetition index.</param>
        /// <param name="componentIndex">Component index.</param>
        /// <param name="subcomponentIndex">Subcomponent index.</param>
        /// <param name="value">New value.</param>
        /// <returns>This SegmentBuilder, for chaining purposes.</returns>
        public ISegmentBuilder Subcomponent(int fieldIndex, int repetition, int componentIndex, int subcomponentIndex,
            string value)
        {
            this[fieldIndex].Subcomponent(repetition, componentIndex, subcomponentIndex, value);
            return this;
        }

        /// <summary>
        ///     Replace all subcomponents within a component.
        /// </summary>
        /// <param name="fieldIndex">Field index.</param>
        /// <param name="repetition">Field repetition index.</param>
        /// <param name="componentIndex">Component index.</param>
        /// <param name="subcomponents">Subcomponent index.</param>
        /// <returns>This SegmentBuilder, for chaining purposes.</returns>
        public ISegmentBuilder Subcomponents(int fieldIndex, int repetition, int componentIndex,
            params string[] subcomponents)
        {
            this[fieldIndex].Subcomponents(repetition, componentIndex, subcomponents);
            return this;
        }

        /// <summary>
        ///     Set a sequence of subcomponents within a component, beginning at the specified start index.
        /// </summary>
        /// <param name="fieldIndex">Field index.</param>
        /// <param name="repetition">Field repetition index.</param>
        /// <param name="componentIndex">Component index.</param>
        /// <param name="startIndex">Subcomponent index to begin replacing at.</param>
        /// <param name="subcomponents">Values to replace with.</param>
        /// <returns>This SegmentBuilder, for chaining purposes.</returns>
        public ISegmentBuilder Subcomponents(int fieldIndex, int repetition, int componentIndex, int startIndex,
            params string[] subcomponents)
        {
            this[fieldIndex].Subcomponents(repetition, componentIndex, startIndex, subcomponents);
            return this;
        }

        public string GetValue(int field = -1, int repetition = -1, int component = -1, int subcomponent = -1)
        {
            return field < 0
                ? Value
                : this[field].GetValue(repetition, component, subcomponent);
        }

        public IEnumerable<string> GetValues(int field = -1, int repetition = -1, int component = -1,
            int subcomponent = -1)
        {
            return field < 0
                ? Values
                : this[field].GetValues(repetition, component, subcomponent);
        }

        public override IElement Clone()
        {
            return new SegmentBuilder(Ancestor, Index, Value);
        }

        ISegment ISegment.Clone()
        {
            return new SegmentBuilder(Ancestor, Index, Value);
        }

        public override IEncodedTypeConverter As
        {
            get { return new BuilderCodec(this); }
        }

        public override char Delimiter
        {
            get { return FieldDelimiter; }
        }

        /// <summary>
        ///     Method that is called when a descendant type field has changed.
        /// </summary>
        /// <param name="oldValue">Old type field value.</param>
        /// <param name="newValue">New type field value.</param>
        private void OnTypeFieldModified(string oldValue, string newValue)
        {
            if (oldValue != null && oldValue != newValue && (newValue == "MSH" || oldValue == "MSH"))
            {
                throw new BuilderException(ErrorCode.ChangingSegmentTypesToAndFromMshIsNotSupported);
            }
        }

        /// <summary>
        ///     Copy the contents of this builder to a string.
        /// </summary>
        /// <returns>Converted segment.</returns>
        public override string ToString()
        {
            return Value;
        }

        protected override IElement GetGenericElement(int index)
        {
            return this[index];
        }
    }
}