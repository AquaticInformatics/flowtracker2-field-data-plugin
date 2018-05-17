using System;
using System.Text;

namespace FlowTracker2Converter
{
    public class TextTable
    {
        private int[] Widths { get; }
        private StringBuilder Builder { get; }

        public TextTable(params int[] widths)
        {
            Widths = widths;
            Builder = new StringBuilder();
        }

        public void AddRow(params string[] values)
        {
            if (values.Length > Widths.Length)
                throw new ArgumentException($"values.Length={values.Length} exceeds Widths.Length={Widths.Length}");

            for (var i = 0; i < values.Length; ++i)
            {
                var width = Widths[i];
                var value = values[i];

                Builder.AppendFormat(value.PadLeft(width));
            }

            Builder.AppendLine();
        }

        public string Format()
        {
            return Builder.ToString();
        }
    }
}
