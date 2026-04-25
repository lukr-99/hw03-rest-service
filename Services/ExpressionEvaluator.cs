using System.Globalization;

namespace RestHW03.Services;

public sealed class ExpressionEvaluator
{
    public double Evaluate(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            throw new ArgumentException("queryEval must not be empty.");
        }

        var parser = new Parser(expression);
        return parser.Parse();
    }

    private sealed class Parser(string text)
    {
        private int position;

        public double Parse()
        {
            var value = ParseExpression();
            SkipWhitespace();

            if (!IsAtEnd)
            {
                throw new ArgumentException($"Unexpected token '{text[position]}' at position {position}.");
            }

            return value;
        }

        private bool IsAtEnd => position >= text.Length;

        private double ParseExpression()
        {
            var value = ParseTerm();

            while (true)
            {
                SkipWhitespace();

                if (Match('+'))
                {
                    value += ParseTerm();
                    continue;
                }

                if (Match('-'))
                {
                    value -= ParseTerm();
                    continue;
                }

                return value;
            }
        }

        private double ParseTerm()
        {
            var value = ParseFactor();

            while (true)
            {
                SkipWhitespace();

                if (Match('*'))
                {
                    value *= ParseFactor();
                    continue;
                }

                if (Match('/'))
                {
                    var divisor = ParseFactor();
                    if (divisor == 0.0d)
                    {
                        throw new ArgumentException("Division by zero is not allowed.");
                    }

                    value /= divisor;
                    continue;
                }

                return value;
            }
        }

        private double ParseFactor()
        {
            SkipWhitespace();

            if (Match('+'))
            {
                return ParseFactor();
            }

            if (Match('-'))
            {
                return -ParseFactor();
            }

            if (Match('('))
            {
                var value = ParseExpression();
                SkipWhitespace();

                if (!Match(')'))
                {
                    throw new ArgumentException("Missing closing parenthesis.");
                }

                return value;
            }

            return ParseNumber();
        }

        private double ParseNumber()
        {
            SkipWhitespace();
            var start = position;

            while (!IsAtEnd && char.IsDigit(text[position]))
            {
                position++;
            }

            if (start == position)
            {
                throw new ArgumentException($"Expected an integer constant at position {position}.");
            }

            var token = text[start..position];
            return double.Parse(token, CultureInfo.InvariantCulture);
        }

        private bool Match(char expected)
        {
            if (IsAtEnd || text[position] != expected)
            {
                return false;
            }

            position++;
            return true;
        }

        private void SkipWhitespace()
        {
            while (!IsAtEnd && char.IsWhiteSpace(text[position]))
            {
                position++;
            }
        }
    }
}
