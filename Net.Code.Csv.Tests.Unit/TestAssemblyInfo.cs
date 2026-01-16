using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
[assembly: TestCollectionOrderer("Net.Code.Csv.Tests.Unit.TestCollectionOrderer", "Net.Code.Csv.Tests.Unit")]
