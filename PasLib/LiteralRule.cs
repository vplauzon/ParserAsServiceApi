﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PasLib
{
    internal class LiteralRule : RuleBase
    {
        private readonly char[] _literal;

        public LiteralRule(string ruleName, IEnumerable<char> literal)
            : base(ruleName)
        {
            if (literal == null || !literal.Any())
            {
                throw new ArgumentNullException(nameof(literal));
            }

            _literal = literal.ToArray();
        }

        public LiteralRule(string ruleName, string literal)
            : base(ruleName)
        {
            if (string.IsNullOrEmpty(literal))
            {
                throw new ArgumentNullException(nameof(literal));
            }

            _literal = literal.ToCharArray();
        }

        protected override IEnumerable<RuleMatch> OnMatch(SubString text, int depth)
        {
            if (text.HasContent
                && text.Length >= _literal.Length
                && text.Enumerate().Take(_literal.Length).SequenceEqual(_literal))
            {
                return new[] { new RuleMatch(this, text.Take(_literal.Length)) };
            }
            else
            {
                return EmptyMatch;
            }
        }

        public override string ToString()
        {
            return "<" + RuleName + "> (\"" + new string(_literal).Replace("\"", "\\\"") + "\")";
        }
    }
}