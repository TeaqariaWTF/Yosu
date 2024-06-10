﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Yosu.Core.Utils;

public static class Memo
{
    private static class ForValue<T>
    {
        private static readonly ConditionalWeakTable<object, Dictionary<int, T>> CacheManifest = [];

        public static Dictionary<int, T> GetCacheForOwner(object owner) =>
            CacheManifest.GetOrCreateValue(owner);
    }

    public static T Cache<T>(object owner, Func<T> getValue)
    {
        var cache = ForValue<T>.GetCacheForOwner(owner);
        var key = getValue.Method.GetHashCode();

        if (cache.TryGetValue(key, out var cachedValue))
            return cachedValue;

        var value = getValue();
        cache[key] = value;

        return value;
    }
}
