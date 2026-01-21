namespace JD.Efcpt.Build.Definitions.Constants;

/// <summary>
/// Common target dependency chains to eliminate duplication
/// </summary>
public static class PipelineConstants
{
    // Core resolution chain
    public static string ResolveChain => string.Join(";",
        EfcptTargets.EfcptResolveInputs,
        EfcptTargets.EfcptEnsureDacpac,
        EfcptTargets.EfcptUseDirectDacpac);
    
    // Full pre-generation chain
    public static string PreGenChain => string.Join(";",
        EfcptTargets.EfcptResolveInputs,
        EfcptTargets.EfcptEnsureDacpac,
        EfcptTargets.EfcptUseDirectDacpac,
        EfcptTargets.EfcptResolveDbContextName);
    
    // Staging chain
    public static string StagingChain => string.Join(";",
        EfcptTargets.EfcptResolveInputs,
        EfcptTargets.EfcptEnsureDacpac,
        EfcptTargets.EfcptUseDirectDacpac,
        EfcptTargets.EfcptResolveDbContextName,
        EfcptTargets.EfcptStageInputs);
    
    // Full generation pipeline
    public static string FullPipeline => string.Join(";",
        EfcptTargets.EfcptResolveInputs,
        EfcptTargets.EfcptUseDirectDacpac,
        EfcptTargets.EfcptEnsureDacpac,
        EfcptTargets.EfcptStageInputs,
        EfcptTargets.EfcptComputeFingerprint,
        EfcptTargets.EfcptGenerateModels,
        EfcptTargets.EfcptCopyDataToDataProject);
}
