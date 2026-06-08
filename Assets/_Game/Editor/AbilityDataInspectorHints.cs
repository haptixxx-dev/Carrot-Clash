using UnityEditor;
using UnityEngine;

namespace CarrotClash.EditorTools
{
    /// <summary>
    /// Custom inspector for <see cref="AbilityDataSO"/> that draws the default fields and then shows,
    /// in a HelpBox, which concrete <see cref="AbilityBase"/> behaviour the current
    /// <see cref="AbilityDataSO.abilityName"/> resolves to via the factory's canonical name map. Makes
    /// it obvious when a designer has typed a name that won't bind to any behaviour.
    /// </summary>
    [CustomEditor(typeof(AbilityDataSO))]
    public class AbilityDataInspectorHints : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            AbilityDataSO data = (AbilityDataSO)target;
            string behaviour = AbilityCatalog.ResolveBehaviour(data.abilityName);

            EditorGUILayout.Space();

            if (string.IsNullOrEmpty(data.abilityName))
            {
                EditorGUILayout.HelpBox(
                    "No ability name set. The AbilityFactory keys on 'abilityName' — it must match a " +
                    "canonical name exactly (e.g. \"Sprint Dash\").",
                    MessageType.Warning);
            }
            else if (behaviour != null)
            {
                EditorGUILayout.HelpBox(
                    $"\"{data.abilityName}\" resolves to behaviour: {behaviour}\n" +
                    $"Kind: {data.kind}",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"\"{data.abilityName}\" does NOT resolve to any AbilityFactory behaviour.\n" +
                    "Fix the name to one of the 16 canonical abilities, or this ability will fail to bind " +
                    "at runtime (AbilityFactory.Attach returns null).",
                    MessageType.Error);
            }
        }
    }
}
