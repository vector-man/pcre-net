﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using PCRE.Wrapper;

namespace PCRE
{
    public sealed class PcreMatch : IPcreGroup, IPcreGroupList
    {
        private readonly object _result; // See remark about JIT in PcreRegex
        private readonly PcreGroup[] _groups;

        internal PcreMatch(MatchData result)
        {
            _result = result;
            _groups = new PcreGroup[result.Regex.CaptureCount + 1];
        }

        private MatchData InternalResult
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return (MatchData)_result; }
        }

        public int CaptureCount
        {
            get { return InternalResult.Regex.CaptureCount; }
        }

        public PcreGroup this[int index]
        {
            get { return GetGroup(index); }
        }

        public PcreGroup this[string name]
        {
            get { return GetGroup(name); }
        }

        internal string Subject
        {
            get { return InternalResult.Subject; }
        }

        public int Index
        {
            get { return this[0].Index; }
        }

        public int EndIndex
        {
            get { return this[0].EndIndex; }
        }

        public int Length
        {
            get { return this[0].Length; }
        }

        public string Value
        {
            get { return this[0].Value; }
        }

        public bool Success
        {
            get { return InternalResult.ResultCode == MatchResultCode.Success; }
        }

        public string Mark
        {
            get { return InternalResult.Mark; }
        }

        public IPcreGroupList Groups
        {
            get { return this; }
        }

        public bool IsPartialMatch
        {
            get { return InternalResult.ResultCode == MatchResultCode.Partial; }
        }

        public IEnumerator<PcreGroup> GetEnumerator()
        {
            return GetAllGroups().GetEnumerator();
        }

        private IEnumerable<PcreGroup> GetAllGroups()
        {
            for (var i = 0; i <= CaptureCount; ++i)
                yield return this[i];
        }

        private PcreGroup GetGroup(int index)
        {
            if (index < 0 || index > CaptureCount)
                return null;

            var group = _groups[index];
            if (group == null)
                _groups[index] = group = CreateGroup(index);

            return group;
        }

        private PcreGroup CreateGroup(int index)
        {
            var result = InternalResult;

            if (result.ResultCode == MatchResultCode.Partial && index != 0)
                return PcreGroup.Empty;

            var uindex = (uint)index;
            var startOffset = result.GetStartOffset(uindex);
            if (startOffset >= 0)
                return new PcreGroup(result.Subject, startOffset, result.GetEndOffset(uindex));

            return PcreGroup.Empty;
        }

        private PcreGroup GetGroup(string name)
        {
            var map = InternalResult.Regex.CaptureNames;
            if (map == null)
                return null;

            int[] indexes;
            if (!map.TryGetValue(name, out indexes))
                return null;

            if (indexes.Length == 1)
                return GetGroup(indexes[0]);

            foreach (var index in indexes)
            {
                var group = GetGroup(index);
                if (group != null && group.Success)
                    return group;
            }

            return PcreGroup.Empty;
        }

        public IEnumerable<PcreGroup> GetDuplicateNamedGroups(string name)
        {
            var map = InternalResult.Regex.CaptureNames;
            if (map == null)
                yield break;

            int[] indexes;
            if (!map.TryGetValue(name, out indexes))
                yield break;

            foreach (var index in indexes)
            {
                var group = GetGroup(index);
                if (group != null)
                    yield return group;
            }
        }

        internal int GetStartOfNextMatchIndex()
        {
            // It's possible to have EndIndex < Index
            // when the pattern contains \K in a lookahead
            return Math.Max(Index, EndIndex);
        }

        public override string ToString()
        {
            return Value;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        int IReadOnlyCollection<PcreGroup>.Count
        {
            get { return CaptureCount + 1; }
        }
    }
}
