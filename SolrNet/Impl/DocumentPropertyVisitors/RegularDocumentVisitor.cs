#region license
// Copyright (c) 2007-2010 Mauricio Scheffer
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
//  
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace SolrNet.Impl.DocumentPropertyVisitors {
    /// <summary>
    /// Pass-through document visitor
    /// </summary>
    public class RegularDocumentVisitor : ISolrDocumentPropertyVisitor {
        private readonly ISolrFieldParser parser;
        private readonly IReadOnlyMappingManager mapper;

        /// <summary>
        /// Pass-through document visitor
        /// </summary>
        /// <param name="parser"></param>
        /// <param name="mapper"></param>
        public RegularDocumentVisitor(ISolrFieldParser parser, IReadOnlyMappingManager mapper) {
            this.parser = parser;
            this.mapper = mapper;
        }

        /// <inheritdoc />
        public void Visit(object doc, string fieldName, XElement field) {
            var allFields = mapper.GetFields(doc.GetType());
            SolrFieldModel thisField;
            if (!allFields.TryGetValue(fieldName, out thisField))
                return;
            if (!thisField.Property.CanWrite)
                return;
            if (parser.CanHandleSolrType(field.Name.LocalName) &&
                parser.CanHandleType(thisField.Property.PropertyType)) {
                var v = parser.Parse(field, thisField.Property.PropertyType);
                try {
                    var check = IsGeneric(thisField.Property, v);
                    if (check.Item1)
                    {
                        if (((System.Collections.ArrayList)v).Count == 1)
                        {
                            thisField.Property.SetValue(doc, check.Item2, null);
                        }
                    }
                    else
                    {
                        var val = ConvertValue(thisField.Property, v);
                        if (val != null)
                            thisField.Property.SetValue(doc, val, null);
                        else
                            thisField.Property.SetValue(doc, v, null);
                    }
                } catch (ArgumentException e) {
                    throw new ArgumentException(string.Format("Could not convert value '{0}' to property '{1}' of document type {2}", v, thisField.Property.Name, thisField.Property.DeclaringType), e);
                }
            }
        }

        private (bool,object) IsGeneric(PropertyInfo item, object v)
        {
            (bool,object) result = (false,null);

            try
            {
                if (v.GetType().Name.ToLower() != "arraylist")
                    return result;
                object val = ((System.Collections.ArrayList)v)[0];

                var value = ConvertValue(item, val);
                if (value != null)
                {
                    result.Item1 = true;
                    result.Item2 = value;
                }
                
            }
            catch (Exception ex)
            {
                throw ex;
            }
           

            return result;

        }

        private object ConvertValue(PropertyInfo item, object value)
        {
            object result = null;

            var objectType = IsNullableType(item.PropertyType) ? Nullable.GetUnderlyingType(item.PropertyType) : item.PropertyType;
            if (item.PropertyType == typeof(String))
                result = value.ToString();
            else if (item.PropertyType == typeof(DateTime) || item.PropertyType == typeof(DateTime?))
                result = Convert.ToDateTime(value.ToString());
            else if (item.PropertyType == typeof(DateTimeOffset) || item.PropertyType == typeof(DateTimeOffset?))
                result = Convert.ChangeType(value, objectType);
            else if (item.PropertyType == typeof(Int16) || item.PropertyType == typeof(Int16?))
                result = Convert.ChangeType(value, objectType);
            else if (item.PropertyType == typeof(Int32) || item.PropertyType == typeof(Int32?))
                result = Convert.ChangeType(value, objectType);
            else if (item.PropertyType == typeof(Int64) || item.PropertyType == typeof(Int64?))
                result = Convert.ChangeType(value, objectType);
            else if (item.PropertyType == typeof(IntPtr) || item.PropertyType == typeof(IntPtr?))
                result = Convert.ChangeType(value, objectType);
            else if (item.PropertyType == typeof(Boolean) || item.PropertyType == typeof(Boolean?))
                result = GetBooleanValue(value);
            else if (item.PropertyType == typeof(Decimal) || item.PropertyType == typeof(Decimal?))
                result = Convert.ToDecimal(value);
            else if (item.PropertyType == typeof(Byte) || item.PropertyType == typeof(Byte?))
                result = Convert.ChangeType(value, objectType);
            else if (item.PropertyType == typeof(Char) || item.PropertyType == typeof(Char?))
                result = Convert.ChangeType(value, objectType);
            else if (item.PropertyType == typeof(Double) || item.PropertyType == typeof(Double?))
                result = Convert.ToDouble(value);
            else if (item.PropertyType == typeof(Guid) || item.PropertyType == typeof(Guid?))
                result = Guid.Parse(value.ToString());
            else if (item.PropertyType == typeof(TimeSpan) || item.PropertyType == typeof(TimeSpan?))
                result = GetTimeSpanValue(value, item.Name);
            else if (item.PropertyType == typeof(UInt16) || item.PropertyType == typeof(UInt16?))
                result = Convert.ChangeType(value, objectType);
            else if (item.PropertyType == typeof(UInt32) || item.PropertyType == typeof(UInt32?))
                result = Convert.ChangeType(value, objectType);
            else if (item.PropertyType == typeof(UInt64) || item.PropertyType == typeof(UInt64?))
                result = Convert.ChangeType(value, objectType);
            else if (item.PropertyType == typeof(UIntPtr) || item.PropertyType == typeof(UIntPtr?))
                result = Convert.ChangeType(value, objectType);
            else if (item?.PropertyType?.BaseType == typeof(Enum))
                result = Enum.Parse(objectType, value.ToString());
            else if (Nullable.GetUnderlyingType(item.PropertyType)?.IsEnum == true)
                result = Enum.Parse(objectType, value.ToString());

            return result;
        }

        private static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>));
        }

        private TimeSpan GetTimeSpanValue(object val, string name)
        {
            bool check = TimeSpan.TryParse(val.ToString(), out TimeSpan temp);
            if (check)
                return temp;
            else
                throw new Exception($"Invalid cast {name} to 'System.TimeSpan'.");
        }
        private bool GetBooleanValue(object val)
        {
            if (val?.ToString().ToLower() == "true" || val?.ToString().ToLower() == "false")
                return Convert.ToBoolean(val);
            else
                return Convert.ToBoolean(Convert.ToInt32(val));
        }
    }
}
