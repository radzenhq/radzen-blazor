// Radzen.Blazor.TrimRuntimeTest - deterministic runtime trimming gate (TRIMMING_AUDIT.md, family H1-H4).
//
// This is a CONSUMER of Radzen.Blazor. It instantiates the item-typed generic components with models
// from a SEPARATE <IsTrimmable> assembly - exactly how a real app references a Shared/Models project -
// then inspects, via reflection on the trimmed model types, whether the trimmer PRESERVED the model's
// property getters. Those getters are read by the components only via reflection (string Property=),
// so nothing roots them unless a component flows [DynamicallyAccessedMembers] on its generic parameter.
//
//   RED today (exit 1): the component generic parameters carry no DAM -> the model getters are trimmed.
//   GREEN (exit 0): once the library annotates the item-typed component generics with
//                   [DynamicallyAccessedMembers(PublicProperties | PublicFields)], the trimmer keeps the
//                   model's public members and every getter below survives.
//
// Note: a method that calls dynamic LINQ directly (DynamicExtensions/PropertyAccess) would NOT be a
// valid red->green gate - the audit's fix for those is [RequiresUnreferencedCode] (a warning), which
// does not preserve anything. The closed-generic-component instantiation below is the mechanism the
// DAM fix actually repairs.

using System.Reflection;
using Radzen.Blazor;
using Radzen.Blazor.TrimTest.Models;

// Force the trimmer to see these closed generic instantiations (here the generic argument IS the data
// item type, so DAM on the generic parameter roots the model). Kept alive so they are not optimized out.
object[] rooted =
[
    new RadzenDataGrid<Order>(),
    new RadzenDataList<Order>(),
    new RadzenScheduler<Appt>(),
    new RadzenPivotDataGrid<Sale>(),
    new RadzenDataFilter<Order>(),
];
GC.KeepAlive(rooted);

// (model type, public members the components read by reflection). Only members directly covered by
// DAM on the item-type generic parameter are gated. Customer.City (nested) is intentionally excluded -
// DAM is not transitive; rooting nested types is a documented consumer responsibility (H14).
(Type Type, string[] Members)[] expectations =
[
    (typeof(Order), ["CustomerName", "Total", "Status", "Customer"]),
    (typeof(Appt), ["Start", "End", "Text"]),
    (typeof(Sale), ["Region", "Revenue"]),
];

// Look members up via a runtime VARIABLE (never a constant): the trimmer intrinsically roots
// GetProperty("LiteralName") on a statically-known type, which would mask the very trimming we are
// testing. A variable name cannot be resolved by the trimmer, so the getter survives only if a
// component's DAM annotation preserved it.
var failures = new List<string>();
foreach (var (type, members) in expectations)
{
    foreach (var member in members)
    {
        var property = type.GetProperty(member);
        if (property?.GetMethod == null)
            failures.Add($"{type.Name}.{member}: getter trimmed away");
    }
}

if (failures.Count > 0)
{
    Console.Error.WriteLine(
        "TRIM RUNTIME GATE FAILED - the trimmer removed model members that Radzen components read by "
        + "reflection. Annotate the item-typed component generic parameters with "
        + "[DynamicallyAccessedMembers(PublicProperties | PublicFields)] (start at PagedDataBoundComponent<T>):");
    foreach (var failure in failures)
        Console.Error.WriteLine("  " + failure);
    return 1;
}

Console.WriteLine($"TRIM RUNTIME GATE PASSED - all {expectations.Sum(e => e.Members.Length)} gated model getters survived trimming.");
return 0;
