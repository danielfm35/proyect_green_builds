using System;
using UnityEngine;

[Serializable]
public class LocalizedText
{
    [TextArea]
    public string english;

    [TextArea]
    public string spanish;

    public string GetOrFallback(string fallback)
    {
        string localized = Get(GameTextLocalizer.CurrentLanguage);
        return string.IsNullOrWhiteSpace(localized) ? fallback : localized;
    }

    public string Get(GameLanguage language)
    {
        switch (language)
        {
            case GameLanguage.Spanish:
                return spanish;
            default:
                return english;
        }
    }
}
