﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class JsonWriter
    {
        private const int INDENT_SIZE = 4;

        private TextWriter _writer;
        private int _indent;

        private string Spacer { get { return this.Pretty ? " " : ""; } }
        public bool Pretty { get; set; } 
        public bool WriteBinary { get; set; }

        public JsonWriter(TextWriter writer)
        {
            _writer = writer;
        }

        public void Serialize(BsonValue value)
        {
            _indent = 0;

            this.WriteValue(value);
        }

        private void WriteValue(BsonValue value)
        {
            if (value == null || value.IsNull)
            {
                this.Write("null");
            }
            else if (value.Type == BsonType.Array)
            {
                this.WriteArray(value.AsArray);
            }
            else if (value.Type == BsonType.Object)
            {
                this.WriteObject(value.AsObject);
            }
            else if (value.Type == BsonType.Byte)
            {
                this.Write(value.AsByte.ToString());
            }
            else if (value.Type == BsonType.ByteArray)
            {
                this.WriteExtendDataType("$binary", this.WriteBinary ? Convert.ToBase64String(value.AsByteArray) : "-- " + value.AsByteArray.Length + " bytes --");
            }
            else if (value.Type == BsonType.Char)
            {
                this.WriteString(value.AsChar.ToString());
            }
            else if (value.Type == BsonType.Boolean)
            {
                this.Write(value.AsBoolean.ToString().ToLower());
            }
            else if (value.Type == BsonType.String)
            {
                this.WriteString(value.AsString);
            }
            else if (value.IsNumber)
            {
                this.WriteFormat(string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0}", value.RawValue));
            }
            else if (value.Type == BsonType.DateTime)
            {
                this.WriteExtendDataType("$date", value.AsDateTime.ToString("yyyy-MM-ddTHH:mm:ssK"));
            }
            else if (value.Type == BsonType.Guid)
            {
                this.WriteExtendDataType("$guid", value.AsGuid.ToString());
            }
        }

        private void WriteObject(BsonObject obj)
        {
            var hasData = obj.Keys.Length > 0;

            this.WriteStartBlock("{", hasData);

            var index = 0;

            var keys = new List<string>(obj.Keys);

            // just for add _id as first place
            if (keys.Contains("_id"))
            {
                keys.Remove("_id");
                keys.Insert(0, "_id");
            }

            foreach (var key in keys)
            {
                this.WriteKeyValue(key, obj[key], index++ < obj.Keys.Length - 1);
            }

            this.WriteEndBlock("}", hasData);
        }

        private void WriteArray(BsonArray arr)
        {
            var hasData = arr.Length > 0;

            this.WriteStartBlock("[", hasData);

            for (var i = 0; i < arr.Length; i++)
            {
                var item = arr[i];

                if (!((item.IsObject && item.AsObject.Keys.Length > 0) || (item.IsArray && item.AsArray.Length > 0)))
                {
                    this.WriteIndent();
                }

                this.WriteValue(item);

                if (i < arr.Length - 1)
                {
                    this.Write(",");
                }
                this.WriteNewLine();
            }

            this.WriteEndBlock("]", hasData);
        }

        private void WriteString(string s)
        {
            this.Write("\"");
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\"':
                        this.Write("\\\"");
                        break;
                    case '\\':
                        this.Write("\\\\");
                        break;
                    case '\b':
                        this.Write("\\b");
                        break;
                    case '\f':
                        this.Write("\\f");
                        break;
                    case '\n':
                        this.Write("\\n");
                        break;
                    case '\r':
                        this.Write("\\r");
                        break;
                    case '\t':
                        this.Write("\\t");
                        break;
                    default:
                        int i = (int)c;
                        if (i < 32 || i > 127)
                        {
                            this.WriteFormat("\\u{0:X04}", i);
                        }
                        else
                        {
                            this.Write(c);
                        }
                        break;
                }
            }
            this.Write("\"");
        }

        private void Write(object obj)
        {
            _writer.Write(obj);
        }

        private void WriteFormat(string format, params object[] args)
        {
            _writer.Write(format, args);
        }

        private void WriteExtendDataType(string type, string value)
        {
            this.WriteFormat("{{{0}\"{1}\":{0}\"{2}\"{0}}}", this.Spacer, type, value);
        }

        private void WriteKeyValue(string key, BsonValue value, bool comma)
        {
            this.WriteIndent();
            this.WriteFormat("\"{0}\":{1}", key, this.Spacer);

            if ((value.IsObject && value.AsObject.Keys.Length > 0) || (value.IsArray && value.AsArray.Length > 0))
            {
                this.WriteNewLine();
            }

            this.WriteValue(value);

            if (comma)
            {
                this.Write(",");
            }

            this.WriteNewLine();
        }

        private void WriteStartBlock(string str, bool hasData)
        {
            if (hasData)
            {
                this.WriteIndent();
                this.Write(str);
                this.WriteNewLine();
                _indent++;
            }
            else
            {
                this.Write(str);
            }
        }

        private void WriteEndBlock(string str, bool hasData)
        {
            if (hasData)
            {
                _indent--;
                this.WriteIndent();
                this.Write(str);
            }
            else
            {
                this.Write(str);
            }
        }

        private void WriteNewLine()
        {
            if (this.Pretty)
            {
                _writer.WriteLine();
            }
        }

        private void WriteIndent()
        {
            if (this.Pretty)
            {
                _writer.Write("".PadRight(_indent * INDENT_SIZE, ' '));
            }
        }
    }
}
