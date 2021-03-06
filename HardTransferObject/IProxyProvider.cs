﻿using System;
using System.Collections.Generic;

namespace HardTransferObject
{
    public interface IProxyProvider
    {
        void Declare(Type baseType);
        ProxyMapping GetMapping(Type baseType);
        Dictionary<Type, Type> TypeMap { get; }
        ProxyMapping[] GetMappingChain(Type baseType);
    }
}