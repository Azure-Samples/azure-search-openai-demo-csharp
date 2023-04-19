// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services.Skills;

public sealed class UpdateContextVariableSkill
{
    [SKFunction("Update knowledge")]
    [SKFunctionName("UpdateKnowledgeVariable")]
    [SKFunctionInput(Description = "The value to add or append")]
    [SKFunctionContextParameter(Name = "knowledge", Description = "variable to update")]
    public void AddOrAppend(string variableValue, SKContext context)
    {
        if (context.Variables.ContainsKey("knowledge"))
        {
            context.Variables["knowledge"] = $"{context.Variables["knowledge"]}\r{variableValue}";
        }
        else
        {
            context.Variables["knowledge"] = variableValue;
        }
    }
}
