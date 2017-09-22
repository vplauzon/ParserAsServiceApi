﻿using System;
using System.Collections.Generic;
using System.Text;

namespace PasLib
{
    internal class RuleProxy : IRule
    {
        private IRule _referencedRule = null;

        public IRule ReferencedRule
        {
            get
            {
                if (_referencedRule == null)
                {
                    throw new InvalidOperationException("Rule isn't set");
                }

                return _referencedRule;
            }
            set
            {
                if (_referencedRule != null)
                {
                    throw new InvalidOperationException("Can't set the referenced rule twice");
                }
                if (value == null)
                {
                    throw new InvalidOperationException("Can't set the referenced rule to null");
                }
                _referencedRule = value;
            }
        }

        string IRule.RuleName => _referencedRule.RuleName;

        RuleResult IRule.Match(SubString text, int depth)
            => _referencedRule.Match(text, depth);
    }
}