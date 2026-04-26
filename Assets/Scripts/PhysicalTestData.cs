using System;
using System.Collections.Generic;

[Serializable]
public class PhysicalTestDataConfig
{
    public List<BaseScoreThreshold> baseScoreThresholds;
    public List<GradeThreshold> gradeThresholds;
    public List<GradeEffect> gradeEffects;
    public List<PhysicalTestDefinition> tests;
}

[Serializable]
public class BaseScoreThreshold
{
    public int minPhysique;
    public int baseScore;
}

[Serializable]
public class GradeThreshold
{
    public int minScore;
    public string grade;
}

[Serializable]
public class GradeEffect
{
    public string grade;
    public int moodDelta;
    public int stressDelta;
}

[Serializable]
public class PhysicalTestDefinition
{
    public string id;
    public string name;
    public string description;
    public BaseDistanceFormula baseDistanceFormula;
    public List<ScoreMapping> scoreMapping;
    public List<TestPhaseDefinition> phases;
}

[Serializable]
public class BaseDistanceFormula
{
    public float physiqueMultiplier;
    public float baseOffset;
}

[Serializable]
public class ScoreMapping
{
    public float minDistance;
    public int score;
}

[Serializable]
public class TestPhaseDefinition
{
    public string name;
    public List<TestStrategyDefinition> strategies;
}

[Serializable]
public class AttributeEffects
{
    public int guilt;
    public int mood;
}

[Serializable]
public class TestStrategyDefinition
{
    public string type;
    public string name;
    public string description;
    
    // For Run & Strength
    public int bonus;
    public bool hasSideEffect;
    public string sideEffectKey;
    public bool conditionalOnSideStitch;
    public float sideStitchSuccessRate;
    public int sideStitchSuccessBonus;
    public int sideStitchFailBonus;
    
    public bool hasRandomOutcome;
    public float successRate;
    public int successBonus;
    public int failBonus;
    public AttributeEffects successAttributeEffects;
    public AttributeEffects failAttributeEffects;

    // For Jump
    public float jumpVarianceMin;
    public float jumpVarianceMax;
    public float successDistanceBonus;
    public bool foulOnFail;
    public float jumpDistancePenalty;
}
