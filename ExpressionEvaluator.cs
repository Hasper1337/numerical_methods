using System;
using System.Collections.Generic;
using System.Globalization;

namespace GraphPlotter
{
    /// <summary>
    /// Исключение, возникающее при ошибке разбора математического выражения.
    /// </summary>
    public class ExpressionParseException : Exception
    {
        public ExpressionParseException(string message) : base(message) { }
    }

    /// <summary>
    /// Разбирает и вычисляет аналитически заданную функцию одной переменной x.
    /// Поддерживает: + - * / ^, скобки, унарный минус, неявное умножение (2x, 2(x+1)),
    /// стандартные элементарные функции (sin, cos, tg, ctg, arcsin, arccos, arctg,
    /// sqrt, ln, lg, exp, abs) и константы pi, e.
    /// </summary>
    public class ExpressionEvaluator
    {
        private static readonly HashSet<string> KnownFunctions = new(StringComparer.OrdinalIgnoreCase)
        {
            "sin", "cos", "tg", "tan", "ctg", "cot",
            "asin", "arcsin", "acos", "arccos",
            "atan", "arctan", "artg", "arcctg", "actg",
            "sqrt", "ln", "lg", "log", "exp", "abs"
        };

        private readonly string _expr;
        private int _pos;
        private double _x;

        public ExpressionEvaluator(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                throw new ExpressionParseException("Выражение не задано.");

            // Убираем пробелы/табуляции, приводим запятую в числе к точке.
            _expr = expression.Replace(" ", "").Replace("\t", "").Replace(",", ".");
        }

        /// <summary>
        /// Проверяет выражение на корректность (бросает исключение при ошибке),
        /// не требуя конкретного значения x.
        /// </summary>
        public void Validate()
        {
            Evaluate(1.2345);
        }

        /// <summary>
        /// Вычисляет значение функции в точке x.
        /// </summary>
        public double Evaluate(double x)
        {
            _pos = 0;
            _x = x;
            double result = ParseExpression();
            SkipNothing();
            if (_pos < _expr.Length)
            {
                throw new ExpressionParseException(
                    $"Неожиданный символ '{_expr[_pos]}' в позиции {_pos + 1}.");
            }
            return result;
        }

        // expression := term (('+' | '-') term)*
        private double ParseExpression()
        {
            double value = ParseTerm();
            while (true)
            {
                char c = Peek();
                if (c == '+') { _pos++; value += ParseTerm(); }
                else if (c == '-') { _pos++; value -= ParseTerm(); }
                else break;
            }
            return value;
        }

        // term := power ( ('*' | '/' | implicit) power )*
        private double ParseTerm()
        {
            double value = ParsePower();
            while (true)
            {
                char c = Peek();
                if (c == '*') { _pos++; value *= ParsePower(); }
                else if (c == '/')
                {
                    _pos++;
                    double divisor = ParsePower();
                    value /= divisor;
                }
                else if (CanStartPrimary(c))
                {
                    // неявное умножение: 2x, 3(x+1), (x+1)(x-1), 2sin(x)
                    value *= ParsePower();
                }
                else break;
            }
            return value;
        }

        // power := unary ('^' power)?   (правоассоциативно)
        private double ParsePower()
        {
            double baseValue = ParseUnary();
            if (Peek() == '^')
            {
                _pos++;
                double exponent = ParsePower();
                return Math.Pow(baseValue, exponent);
            }
            return baseValue;
        }

        // unary := ('-' | '+')* primary
        private double ParseUnary()
        {
            char c = Peek();
            if (c == '-') { _pos++; return -ParseUnary(); }
            if (c == '+') { _pos++; return ParseUnary(); }
            return ParsePrimary();
        }

        // primary := NUMBER | IDENT | IDENT '(' expression ')' | '(' expression ')'
        private double ParsePrimary()
        {
            char c = Peek();

            if (c == '(')
            {
                _pos++;
                double value = ParseExpression();
                Expect(')');
                return value;
            }

            if (char.IsDigit(c) || c == '.')
            {
                return ParseNumber();
            }

            if (char.IsLetter(c))
            {
                string ident = ParseIdentifier();
                if (Peek() == '(')
                {
                    _pos++;
                    double arg = ParseExpression();
                    Expect(')');
                    return ApplyFunction(ident, arg);
                }
                return ApplyConstantOrVariable(ident);
            }

            throw new ExpressionParseException(
                _pos < _expr.Length
                    ? $"Неожиданный символ '{_expr[_pos]}' в позиции {_pos + 1}."
                    : "Неожиданный конец выражения.");
        }

        private double ParseNumber()
        {
            int start = _pos;
            bool dotSeen = false;
            while (_pos < _expr.Length && (char.IsDigit(_expr[_pos]) || (_expr[_pos] == '.' && !dotSeen)))
            {
                if (_expr[_pos] == '.') dotSeen = true;
                _pos++;
            }
            string token = _expr.Substring(start, _pos - start);
            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                throw new ExpressionParseException($"Некорректное число: '{token}'.");
            return result;
        }

        private string ParseIdentifier()
        {
            int start = _pos;
            while (_pos < _expr.Length && char.IsLetter(_expr[_pos]))
                _pos++;
            return _expr.Substring(start, _pos - start);
        }

        private double ApplyFunction(string name, double arg)
        {
            switch (name.ToLowerInvariant())
            {
                case "sin": return Math.Sin(arg);
                case "cos": return Math.Cos(arg);
                case "tg":
                case "tan": return Math.Tan(arg);
                case "ctg":
                case "cot": return 1.0 / Math.Tan(arg);
                case "asin":
                case "arcsin": return Math.Asin(arg);
                case "acos":
                case "arccos": return Math.Acos(arg);
                case "atan":
                case "arctan":
                case "artg": return Math.Atan(arg);
                case "arcctg":
                case "actg": return Math.PI / 2 - Math.Atan(arg);
                case "sqrt": return Math.Sqrt(arg);
                case "ln": return Math.Log(arg);
                case "lg":
                case "log": return Math.Log10(arg);
                case "exp": return Math.Exp(arg);
                case "abs": return Math.Abs(arg);
                default:
                    throw new ExpressionParseException($"Неизвестная функция: '{name}'.");
            }
        }

        private double ApplyConstantOrVariable(string ident)
        {
            switch (ident.ToLowerInvariant())
            {
                case "x": return _x;
                case "pi": return Math.PI;
                case "e": return Math.E;
                default:
                    if (KnownFunctions.Contains(ident))
                        throw new ExpressionParseException($"Функция '{ident}' должна использоваться со скобками, например {ident}(x).");
                    throw new ExpressionParseException($"Неизвестный идентификатор: '{ident}'.");
            }
        }

        private bool CanStartPrimary(char c)
        {
            return c != '\0' && (char.IsDigit(c) || c == '.' || char.IsLetter(c) || c == '(');
        }

        private char Peek() => _pos < _expr.Length ? _expr[_pos] : '\0';

        private void Expect(char c)
        {
            if (Peek() != c)
            {
                throw new ExpressionParseException(
                    _pos < _expr.Length
                        ? $"Ожидался символ '{c}' в позиции {_pos + 1}, найден '{_expr[_pos]}'."
                        : $"Ожидался символ '{c}', но выражение закончилось.");
            }
            _pos++;
        }

        private void SkipNothing() { /* пробелы уже удалены в конструкторе */ }
    }
}
