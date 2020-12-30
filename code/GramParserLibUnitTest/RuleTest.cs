using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GramParserLib;
using GramParserLib.Rule;
using System.Collections.Immutable;

namespace GramParserLibUnitTest
{
    [TestClass]
    public class RuleTest
    {
        #region Trivial
        [TestMethod]
        public void EmptyNone()
        {
            var rule = MatchNoneRule.Instance;
            var match = rule.Match(new ExplorerContext("")).FirstOrDefault();

            Assert.IsNull(match);
        }

        [TestMethod]
        public void EmptyAny()
        {
            var rule = new MatchAnyCharacterRule(null, null) as IRule;
            var match = rule.Match(new ExplorerContext("")).FirstOrDefault();

            Assert.IsNull(match);
        }
        #endregion

        #region Any
        [TestMethod]
        public void Any()
        {
            var rule = new MatchAnyCharacterRule("Any", null) as IRule;
            var samples = new[]
            {
                "g",
                "K",
                "@",
                "*",
                "/"
            };

            for (int i = 0; i != samples.Length; ++i)
            {
                var match =
                    rule.Match(new ExplorerContext(samples[i])).FirstOrDefault();

                Assert.IsNotNull(match, $"Success - {i}");
                Assert.AreEqual(rule.RuleName, match.Rule.RuleName, $"Rule - {i}");
                Assert.AreEqual(samples[i].Length, match.Text.Length, $"MatchLength - {i}");
                Assert.AreEqual(1, match.Text.Length, $"Content - {i}");
            }
        }
        #endregion

        #region Literal
        [TestMethod]
        public void Literal()
        {
            var rule = new LiteralRule("Lit", null, "great") as IRule;
            var match = rule.Match(new ExplorerContext("great")).FirstOrDefault();
            var nomatch = rule.Match(new ExplorerContext("h")).FirstOrDefault();

            Assert.IsNotNull(match, "Success");
            Assert.AreEqual(rule.RuleName, match.Rule.RuleName, "Rule");
            Assert.AreEqual(5, match.Text.Length, "Content");

            Assert.IsNull(nomatch, "Failure");
        }
        #endregion

        #region Range
        [TestMethod]
        public void Range()
        {
            var samples = new[]
            {
                Tuple.Create('a', 'e', 'c', true),
                Tuple.Create('a', 'e', 'e', true),
                Tuple.Create('a', 'e', 'a', true),
                Tuple.Create('a', 'e', 'f', false),
                Tuple.Create('b', 'e', 'a', false),
                Tuple.Create('b', 'e', 'C', false)
            };

            for (int i = 0; i != samples.Length; ++i)
            {
                var first = samples[i].Item1;
                var last = samples[i].Item2;
                var test = samples[i].Item3;
                var isSuccess = samples[i].Item4;
                var rule = new RangeRule("Range", null, first, last) as IRule;
                var match =
                    rule.Match(new ExplorerContext(test.ToString())).FirstOrDefault();

                if (isSuccess)
                {
                    Assert.IsNotNull(match, $"Success - {i}");
                    Assert.AreEqual(rule.RuleName, match.Rule.RuleName, $"Rule - {i}");
                    Assert.AreEqual(1, match.Text.Length, $"MatchLength - {i}");
                }
                else
                {
                    Assert.IsNull(match, $"Failure - {i}");
                }
            }
        }
        #endregion

        #region Repeat
        [TestMethod]
        public void EmptyRepeat()
        {
            var oneCharRule = new LiteralRule("oneChar", null, "g");
            var rule = new RepeatRule("Repeat", null, oneCharRule, null, null) as IRule;
            var match = rule.Match(new ExplorerContext("")).FirstOrDefault();

            Assert.IsNotNull(match, "Success");
            Assert.AreEqual(rule.RuleName, match.Rule.RuleName, "Rule");
            Assert.AreEqual(0, match.Text.Length, "MatchLength");
            Assert.AreEqual(0, ToList(match.ComputeOutput()).Count(), "Contents");
        }

        [TestMethod]
        public void RepeatOneCharacter()
        {
            var oneCharRule = new LiteralRule("oneChar", null, "g");
            var rule = new RepeatRule("Repeat", null, oneCharRule, null, null) as IRule;
            var match = rule.Match(new ExplorerContext("ggggg")).FirstOrDefault();

            Assert.IsNotNull(match, "Success");
            Assert.AreEqual(rule.RuleName, match.Rule.RuleName, "Rule");
            Assert.AreEqual(5, match.Text.Length, "MatchLength");
            Assert.AreEqual(5, ToList(match.ComputeOutput()).Count(), "Contents");
        }

        [TestMethod]
        public void RepeatCharacterWithCardinality()
        {
            var oneCharRule = new LiteralRule("oneChar", null, "g");
            var testSet = new[]
            {
                Tuple.Create<string, int?, int?, bool>("gg", 2, 2, true),
                Tuple.Create<string, int?, int?, bool>("gg", 1, 2, true),
                Tuple.Create<string, int?, int?, bool>("gg", null, 2, true),
                Tuple.Create<string, int?, int?, bool>("gg", 2, null, true),
                Tuple.Create<string, int?, int?, bool>("ggg", 2, 2, false),
                Tuple.Create<string, int?, int?, bool>("ggg", 2, 3, true),
                Tuple.Create<string, int?, int?, bool>("g", 2, 2, false),
                Tuple.Create<string, int?, int?, bool>("g", 2, 3, false)
            };

            for (int i = 0; i != testSet.Length; ++i)
            {
                var text = testSet[i].Item1;
                var min = testSet[i].Item2;
                var max = testSet[i].Item3;
                var isSuccess = testSet[i].Item4;
                var rule = new RepeatRule("Repeat", null, oneCharRule, min, max) as IRule;
                var match = rule.Match(new ExplorerContext(text)).FirstOrDefault();

                if (!isSuccess)
                {
                    Assert.IsTrue(
                        match == null || match.Text.Length != text.Length,
                        $"Test case #{i} should have failed");
                }
                else
                {
                    Assert.IsNotNull(match, $"Success - {i}");
                    Assert.AreEqual(rule.RuleName, match.Rule.RuleName, $"Rule - {i}");
                    Assert.AreEqual(text.Length, match.Text.Length, $"MatchLength - {i}");
                    Assert.AreEqual(text.Length, ToList(match.ComputeOutput()).Count(), $"Contents - {i}");
                }
            }
        }

        [TestMethod]
        public void RepeatWithSequenceAndInterleave()
        {
            var interleave = new RepeatRule("interleave", null, new LiteralRule(null, null, " "), 0, null);
            var seq = new SequenceRule("seq", null, new[]
            {
                new TaggedRule(new LiteralRule(null, null, "|")),
                new TaggedRule("t", new LiteralRule(null, null, "a"))
            });
            var rule = new RepeatRule("rep", null, seq, 1, null);
            var text = "  |a  |a   |a";
            var match = rule.Match(new ExplorerContext(text, interleave)).FirstOrDefault();

            Assert.IsNotNull(match, "Success");
            Assert.AreEqual(rule.RuleName, match.Rule.RuleName, "Rule");
            Assert.AreEqual(text.Length - 1, match.Text.Length, "MatchLength");
            Assert.AreEqual(3, ToList(match.ComputeOutput()).Count(), "Contents");
        }
        #endregion

        #region Disjunction
        [TestMethod]
        public void DisjunctionWithoutTags()
        {
            var samples = new[]
            {
                Tuple.Create("Alice", true),
                Tuple.Create("Bob", true),
                Tuple.Create("Charles", true),
                Tuple.Create("Didier", false)
            };
            //  Alice | Bob | Charles
            var rule = new DisjunctionRule("Disjunction", null, new[]
            {
                new TaggedRule(new LiteralRule("Alice", null, "Alice")),
                new TaggedRule(new LiteralRule("Bob", null, "Bob")),
                new TaggedRule(new LiteralRule("Charles", null, "Charles"))
            });

            for (int i = 0; i != samples.Length; ++i)
            {
                var text = samples[i].Item1;
                var isSuccess = samples[i].Item2;
                var match = rule.Match(new ExplorerContext(text)).FirstOrDefault();

                if (!isSuccess)
                {
                    Assert.IsNull(match, $"Test case #{i} should have failed");
                }
                else
                {
                    Assert.IsNotNull(match, $"Success - {i}");
                    Assert.AreEqual(rule.RuleName, match.Rule.RuleName, $"Rule - {i}");
                    Assert.AreEqual(text.Length, match.Text.Length, $"MatchLength - {i}");
                    Assert.AreEqual(text, match.Text.ToString(), $"Content - {i}");
                }
            }
        }

        [TestMethod]
        public void DisjunctionWithTags()
        {
            var samples = new[]
            {
                ("Alice", true),
                ("Bob", true),
                ("Charles", true),
                ("Didier", false),
                ("Darwin", true),
                ("Ernest", true),
                ("Ephreme", false)
            };
            //  a:Alice | s1:(b:Bob | c:Charles) | s2::(d:Darwin | e:Ernest)
            var subRule1 = new DisjunctionRule("Disjunction", null, new[]
            {
                new TaggedRule("b", new LiteralRule("Bob", null, "Bob")),
                new TaggedRule("c", new LiteralRule("Charles", null, "Charles"))
            });
            var subRule2 = new DisjunctionRule("Disjunction", null, new[]
            {
                new TaggedRule("d", new LiteralRule("Darwin", null, "Darwin")),
                new TaggedRule("e", new LiteralRule("Ernest", null, "Ernest"))
            });
            var rule = new DisjunctionRule("Disjunction", null, new[]
            {
                new TaggedRule("a", new LiteralRule("Alice", null, "Alice")),
                new TaggedRule("s1", subRule1),
                new TaggedRule("s2", subRule2)
            });

            for (int i = 0; i != samples.Length; ++i)
            {
                (var text, var isSuccess) = samples[i];
                var match = rule.Match(new ExplorerContext(text)).FirstOrDefault();

                if (!isSuccess)
                {
                    Assert.IsNull(match, $"Test case #{i} should have failed");
                }
                else
                {
                    Assert.IsNotNull(match, $"Success - {i}");
                    Assert.AreEqual(rule.RuleName, match.Rule.RuleName, $"Rule - {i}");
                    Assert.AreEqual(text.Length, match.Text.Length, $"MatchLength - {i}");

                    var output = ToMap(match.ComputeOutput());

                    Assert.AreEqual(1, output.Count, $"Fragments Count - {i}");
                    if (output.First().Key == "s1")
                    {
                        Assert.AreNotEqual(
                            0,
                            ToMap(output["s1"]).Count,
                            $"Sub Fragments 1 - {i}");
                    }
                    if (output.First().Key == "s2")
                    {
                        Assert.AreEqual(
                            1,
                            ToMap(output["s2"]).Count,
                            $"Sub Fragments 2 - {i}");
                    }
                }
            }
        }

        [TestMethod]
        public void DisjunctionWithRepeat()
        {
            var samples = new[]
            {
                Tuple.Create("ababab", true),
                Tuple.Create("aaaaaa", true),
                Tuple.Create("bbbbbbb", true),
                Tuple.Create("bbbbaaaabbbaaaa", true),
                Tuple.Create("", true),
                Tuple.Create("kaaaaabbbbb", false),
                Tuple.Create("kaaaa", false)
            };
            //  ('a'* | 'b'*)*
            var aRule = new RepeatRule("RepeatA", null, new LiteralRule("A", null, "a"), null, null);
            var bRule = new RepeatRule("RepeatB", null, new LiteralRule("B", null, "b"), null, null);
            var disjunction = new DisjunctionRule("Disjunction", null, new[]
            {
                new TaggedRule(aRule),
                new TaggedRule(bRule)
            });
            var rule = new RepeatRule("MasterRepeat", null, disjunction, null, null);

            for (int i = 0; i != samples.Length; ++i)
            {
                var text = samples[i].Item1;
                var isSuccess = samples[i].Item2;
                var match = rule.Match(new ExplorerContext(text)).FirstOrDefault();

                if (!isSuccess)
                {
                    Assert.IsTrue(
                        match == null || match.Text.Length != text.Length,
                        $"Test case #{i} should have failed");
                }
                else
                {
                    Assert.IsNotNull(match, $"Success - {i}");
                    Assert.AreEqual(rule.RuleName, match.Rule.RuleName, $"Rule - {i}");
                    Assert.AreEqual(text.Length, match.Text.Length, $"MatchLength - {i}");
                }
            }
        }
        #endregion

        #region Sequence
        [TestMethod]
        public void SequenceWithoutTags()
        {
            var rule = new SequenceRule("Seq", null, new[]
            {
                new TaggedRule(new LiteralRule(null, null, "Hi")),
                new TaggedRule(new LiteralRule(null, null, "Bob")),
                new TaggedRule(new LiteralRule(null, null, "!"))
            });
            var text = "HiBob!";
            var match = rule.Match(new ExplorerContext(text)).FirstOrDefault();

            Assert.IsNotNull(match, "Success");
            Assert.AreEqual(rule.RuleName, match.Rule.RuleName, "Seq");
            Assert.AreEqual(text.Length, match.Text.Length, "MatchLength");
            Assert.AreEqual(text.ToString().TrimStart(), match.Text.ToString(), "Content");
            Assert.AreEqual(3, ToList(match.ComputeOutput()).Count, "Fragments");
        }

        [TestMethod]
        public void SequenceWithTags()
        {
            var rule = new SequenceRule("Seq", null, new[]
            {
                new TaggedRule("h", new LiteralRule(null, null, "Hi")),
                new TaggedRule("b", new LiteralRule(null, null, "Bob")),
                new TaggedRule(new LiteralRule(null, null, "!"))
            });
            var text = "HiBob!";
            var match = rule.Match(new ExplorerContext(text)).FirstOrDefault();

            Assert.IsNotNull(match, "Success");
            Assert.AreEqual(rule.RuleName, match.Rule.RuleName, "Seq");
            Assert.AreEqual(text.Length, match.Text.Length, "MatchLength");

            var output = ToMap(match.ComputeOutput());

            Assert.AreEqual(2, output.Count(), "Fragments");
            Assert.AreEqual("Hi", output["h"].ToString(), "Fragments - Hi");
            Assert.AreEqual("Bob", output["b"].ToString(), "Fragments - Bob");
        }

        [TestMethod]
        public void SequenceWithNoChildrenTags()
        {
            var rule = new SequenceRule("Seq", null, new[]
            {
                new TaggedRule("a", new RepeatRule(null, null, new LiteralRule(null, null, "a"), 1, null)),
                new TaggedRule("b", new RepeatRule(null, null, new LiteralRule(null, null, "b"), 1, null))
            });
            var text = "aaaabb";
            var match = rule.Match(new ExplorerContext(text)).FirstOrDefault();

            Assert.IsNotNull(match, "Success");
            Assert.AreEqual(rule.RuleName, match.Rule.RuleName, "Seq");
            Assert.AreEqual(text.Length, match.Text.Length, "MatchLength");

            var output = ToMap(match.ComputeOutput());

            Assert.AreEqual(2, output.Count(), "Fragments");
            Assert.AreEqual(
                4, ToList(output["a"]).Count, "Fragments text - a");
            Assert.AreEqual(
                2, ToList(output["b"]).Count(), "Fragments text - b");
        }
        #endregion

        #region Substract
        [TestMethod]
        public void Substract()
        {
            var samples = new[]
            {
                Tuple.Create('a', true),
                Tuple.Create('z', true),
                Tuple.Create('t', true),
                Tuple.Create('f', false),
                Tuple.Create('g', false),
                Tuple.Create('h', false)
            };
            var range = new RangeRule(null, null, 'a', 'z');
            var exclusion = new RangeRule(null, null, 'f', 'h');
            var rule = new SubstractRule("Substract", null, range, exclusion);

            for (int i = 0; i != samples.Length; ++i)
            {
                var text = samples[i].Item1.ToString();
                var isSuccess = samples[i].Item2;
                var match = rule.Match(new ExplorerContext(text)).FirstOrDefault();

                if (!isSuccess)
                {
                    Assert.IsNull(match, $"Test case #{i} should have failed");
                }
                else
                {
                    Assert.IsNotNull(match, $"Success - {i}");
                    Assert.AreEqual(rule.RuleName, match.Rule.RuleName, $"Rule - {i}");
                    Assert.AreEqual(text.Length, match.Text.Length, $"MatchLength - {i}");
                    Assert.AreEqual(text, match.Text.ToString(), $"Content - {i}");
                }
            }
        }

        [TestMethod]
        public void SubstractWithTags()
        {
            var samples = new[]
            {
                Tuple.Create('a', true),
                Tuple.Create('z', true),
                Tuple.Create('t', true),
                Tuple.Create('f', false),
                Tuple.Create('g', false),
                Tuple.Create('h', false)
            };
            var range = new RangeRule(null, null, 'a', 'z');
            var exclusion = new RangeRule(null, null, 'f', 'h');
            var rule = new SubstractRule(
                "Substract",
                null,
                range,
                exclusion);

            for (int i = 0; i != samples.Length; ++i)
            {
                var text = samples[i].Item1.ToString();
                var isSuccess = samples[i].Item2;
                var match = rule.Match(new ExplorerContext(text)).FirstOrDefault();

                if (!isSuccess)
                {
                    Assert.IsNull(match, $"Test case #{i} should have failed");
                }
                else
                {
                    Assert.IsNotNull(match, $"Success - {i}");
                    Assert.AreEqual(rule.RuleName, match.Rule.RuleName, $"Rule - {i}");
                    Assert.AreEqual(text.Length, match.Text.Length, $"MatchLength - {i}");
                }
            }
        }
        #endregion

        #region Recursion
        [TestMethod]
        public void PotentialInfiniteRecurse()
        {
            //  Equivalent to:
            //  rule A = "a".."z";
            //  rule B = C "," C;
            //  rule C = A | B;
            //  Try to match "a" with C
            var ruleA = new RangeRule("A", null, 'a', 'z');
            var proxyC = new RuleProxy();
            var ruleB = new SequenceRule("B", null, new[]{
                new TaggedRule(proxyC),
                new TaggedRule(new LiteralRule(null, null, ",")),
                new TaggedRule(proxyC)
            });
            var ruleC = new DisjunctionRule("C", null, new[] {
                new TaggedRule(ruleA),
                new TaggedRule(ruleB)
            });

            proxyC.ReferencedRule = ruleC;

            var match = ruleC.Match(new ExplorerContext("a")).FirstOrDefault();

            Assert.IsNotNull(match, "Should be a success");
        }
        #endregion

        private IImmutableList<object> ToList(object output)
        {
            return (IImmutableList<object>)output;
        }

        private IImmutableDictionary<string, object> ToMap(object output)
        {
            return (IImmutableDictionary<string, object>)output;
        }
    }
}