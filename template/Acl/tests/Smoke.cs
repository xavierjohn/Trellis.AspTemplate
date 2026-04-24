// DO NOT DELETE.
//
// Microsoft.Testing.Platform (MTP) returns exit code 8 ("zero tests ran") when
// a test project compiles successfully but contains no [Fact] methods. At the
// solution level, `dotnet test` treats that as a failure for the whole run.
//
// While replacing the TodoSample.* tests with your domain-specific tests, there
// will be a window where this project may have no real tests yet. This file
// guarantees MTP always discovers at least one [Fact] so the slnx-level
// `dotnet test` stays green during incremental scaffold→replace work.
//
// Keep this file. Add new tests around it.
namespace AntiCorruptionLayer.Tests;

public sealed class Smoke
{
    [Fact]
    public void Project_compiles_and_has_at_least_one_test() { }
}
