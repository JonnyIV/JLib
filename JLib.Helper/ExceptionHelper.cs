﻿using System.Text.Json;
using System.Text.Json.Nodes;

namespace JLib.Helper;

public static class ExceptionHelper
{
    /// <summary>
    /// Throws the <paramref name="exception"/> if it is not null.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to be thrown.</param>
    public static void Throw(this Exception? exception)
    {
        if (exception is not null)
            throw exception;
    }

}
