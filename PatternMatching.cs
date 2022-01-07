using System;
using System.Collections;
using System.Collections.Generic;
using static SRXDScoreMod.PatternMatching;

namespace SRXDScoreMod {
    public static class PatternMatching {
        public readonly struct Result {
            public int Start { get; }
            
            public int End { get; }

            public int Length => End - Start;

            public Result(int start, int end) {
                Start = start;
                End = end;
            }
        }
        
        public static PatternMatcher<T> Match<T>(IList<T> list, Func<T, bool>[] pattern, int startIndex = 0, int endIndex = -1) {
            if (endIndex < 0)
                endIndex = list.Count;

            return new NormalPatternMatcher<T>(list, startIndex, endIndex, pattern);
        }

        public static PatternMatcher<T> Then<T>(this PatternMatcher<T> matcher, Func<T, bool>[] pattern) => new ThenPatternMatcher<T>(matcher, pattern);

        private static bool IsMatch<T>(IList<T> list, Func<T, bool>[] pattern, int index, out Result result) {
            for (int i = 0, j = index; i < pattern.Length; i++, j++) {
                if (pattern[i](list[j]))
                    continue;

                result = new Result();
                
                return false;
            }

            result = new Result(index, index + pattern.Length);

            return true;
        }
        
        public abstract class PatternMatcher<T> : IEnumerable<Result> {
            internal IList<T> List { get; }
            internal Func<T, bool>[] Pattern { get; }
            
            protected internal int StartIndex { get; }
            protected internal int EndIndex { get; }

            internal PatternMatcher(IList<T> list, int startIndex, int endIndex, Func<T, bool>[] pattern) {
                List = list;
                StartIndex = startIndex;
                EndIndex = endIndex;
                Pattern = pattern;
            }

            public abstract IEnumerator<Result> GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }
        }

        private class NormalPatternMatcher<T> : PatternMatcher<T> {
            public NormalPatternMatcher(IList<T> list, int startIndex, int endIndex, Func<T, bool>[] pattern) : base(list, startIndex, endIndex, pattern) { }
            
            public override IEnumerator<Result> GetEnumerator() {
                for (int i = StartIndex; i <= EndIndex - Pattern.Length; i++) {
                    if (IsMatch(List, Pattern, i, out var result))
                        yield return result;
                }
            }
        }

        private class ThenPatternMatcher<T> : PatternMatcher<T> {
            private PatternMatcher<T> matcher;

            public ThenPatternMatcher(PatternMatcher<T> matcher, Func<T, bool>[] pattern) : base(matcher.List, matcher.StartIndex, matcher.EndIndex, pattern) {
                this.matcher = matcher;
            }
            
            public override IEnumerator<Result> GetEnumerator() {
                using var enumerator = matcher.GetEnumerator();
                
                if (!enumerator.MoveNext())
                    yield break;

                var current = enumerator.Current;
                Result findResult;

                while (enumerator.MoveNext()) {
                    var next = enumerator.Current;

                    if (Find(current.End, next.Start, out findResult))
                        yield return new Result(current.Start, findResult.End);

                    current = next;
                }
                
                if (Find(current.End, EndIndex, out findResult))
                    yield return new Result(current.Start, findResult.End);

                bool Find(int startIndex, int endIndex, out Result result) {
                    for (int i = startIndex; i <= endIndex - Pattern.Length; i++) {
                        if (IsMatch(List, Pattern, i, out result))
                            return true;
                    }

                    result = new Result();

                    return false;
                }
            }
        }
    }
}
