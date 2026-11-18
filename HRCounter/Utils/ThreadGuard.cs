using System;
using System.Runtime.CompilerServices;
using IPA.Utilities;

namespace HRCounter.Utils;

internal static class ThreadGuard
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EnsureOnMainThread()
    {
        if (!UnityGame.OnMainThread)
        {
            throw new InvalidOperationException("This method must be called on the main thread.");
        }
    }
}
