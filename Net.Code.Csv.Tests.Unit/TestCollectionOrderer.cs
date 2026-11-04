using System;
using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Net.Code.Csv.Tests.Unit;

public sealed class TestCollectionOrderer : ITestCollectionOrderer
{
    public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections)
        => testCollections.OrderBy(collection => collection.DisplayName, StringComparer.Ordinal);
}
