namespace AnatomiQ.Data
{
    /// <summary>
    /// Contract for CORE-008 (Data Layer). Lives in the Data assembly and intentionally does
    /// NOT depend on AnatomiQ.Core: Core already references Data (the ServiceRegistry exposes
    /// IDataLayer), so a back-reference from Data to Core would create a circular assembly
    /// dependency, which Unity forbids. The registry therefore registers the data layer through
    /// its own typed entry point rather than the generic IService path. Empty at scaffold time;
    /// load/lookup members for organ and disease assets are added when CORE-008 is built.
    /// </summary>
    public interface IDataLayer
    {
    }
}
