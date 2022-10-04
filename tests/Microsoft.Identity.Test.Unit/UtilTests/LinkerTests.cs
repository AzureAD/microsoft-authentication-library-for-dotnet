// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !NET6_0
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.UtilTests
{
    [TestClass]
    public class LinkerTests
    {
        [TestMethod]
        public void JsonObjectsArePreserved()
        {
            const string Message = "Please add a [Preserve(AllMembers=true)] attribute to each serializable object to avoid the Xamarin Linker removing this type. Type missing this: ";
            var serializableTypes = typeof(PublicClientApplication).Assembly.GetTypes()
                .Where(
                    t => t.GetCustomAttributes(typeof(JsonObjectAttribute), true).Any());

            foreach (var serializableType in serializableTypes)
            {
                var preserveAttribute = serializableType.GetCustomAttribute<PreserveAttribute>();
                Assert.IsNotNull(preserveAttribute, Message + serializableType);
                Assert.IsTrue(preserveAttribute.AllMembers, Message + serializableType);
            }
        }
    }
}
#endif
