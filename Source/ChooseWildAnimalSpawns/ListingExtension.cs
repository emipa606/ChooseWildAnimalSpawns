using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

[StaticConstructorOnStartup]
public static class ListingExtension
{
    public static bool ListItemSelectable(this Listing lister, string header, Color hoverColor, out Vector2 position,
        bool selected = false, bool visualize = false, string toolTip = null)
    {
        var anchor = Text.Anchor;
        var color = GUI.color;
        var rect = lister.GetRect(24f);
        position = rect.position;
        if (selected)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.5f));
        }
        else
        {
            if (visualize)
            {
                Widgets.DrawBoxSolid(rect, new Color(0.4f, 0.4f, 0.4f, 0.5f));
            }
        }

        if (Mouse.IsOver(rect))
        {
            GUI.color = hoverColor;
        }

        Text.Anchor = TextAnchor.MiddleLeft;
        if (header != null)
        {
            Widgets.Label(rect, header);
            if (toolTip != null)
            {
                TooltipHandler.TipRegion(rect, toolTip);
            }
        }

        Text.Anchor = anchor;
        GUI.color = color;

        if (!Widgets.ButtonInvisible(rect))
        {
            return false;
        }

        SoundDefOf.Click.PlayOneShotOnCamera();
        return true;
    }
}