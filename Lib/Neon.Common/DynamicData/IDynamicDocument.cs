﻿//-----------------------------------------------------------------------------
// FILE:	    IDynamicDocument.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.

using System;

using Neon.Common;
using Neon.DynamicData;

namespace Neon.DynamicData
{
    /// <summary>
    /// Base interface for document classes generated by the <b>gen-entity</b>
    /// tool.
    /// </summary>
    public interface IDynamicDocument
    {
        /// <summary>
        /// Returns the document's link string.
        /// </summary>
        /// <returns>The link string or <c>null</c>.</returns>
        /// <remarks>
        /// <see cref="_GetLink()"/> is used to implement entity linking for environments that
        /// provide an <see cref="IDynamicEntityContext"/> implementation.
        /// </remarks>
        string _GetLink();

        /// <summary>
        /// Returns <c>true</c> if the document has been deleted.
        /// </summary>
        bool IsDeleted { get; }
    }
}