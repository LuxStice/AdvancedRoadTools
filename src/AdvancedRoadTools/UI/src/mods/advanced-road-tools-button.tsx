import { FloatingButton } from "cs2/ui";
import React from 'react';
import {useLocalization} from "cs2/l10n";
import {trigger} from "cs2/api";
import mod from "../../mod.json";
import zoneIcon from "../../images/Zone Controller Tool.svg";
import buttonIcon from "../../images/ToolIcon/ToolsIcon.png";
import {VanillaComponentResolver} from "../YenYang/VanillaComponentResolver";

function ToggleZoneControllerTool()
{
    trigger(mod.id, "ToggleZoneControllerTool");
}

export function descriptionTooltip(tooltipTitle: string | null, tooltipDescription: string | null) : JSX.Element {
    return (
        <>
            <div className={VanillaComponentResolver.instance.descriptionTooltipTheme.title}>{tooltipTitle}</div>
            <div className={VanillaComponentResolver.instance.descriptionTooltipTheme.content}>{tooltipDescription}</div>
        </>
    );
}
function ZoningToolControllerButton(): JSX.Element {

        const {translate} = useLocalization();
        const buttonTooltip = translate("AdvancedRoadTools.Zone_Controller.ToolName", "ACT");
    return (
        
        <FloatingButton
            onClick={ToggleZoneControllerTool}
            src={buttonIcon}
            tooltipLabel={buttonTooltip}
        ></FloatingButton>
    )
}

export default ZoningToolControllerButton;