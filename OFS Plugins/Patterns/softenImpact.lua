package.path = package.path .. ";"..os.getenv('APPDATA').."\\OFS\\OFS3_data\\extensions/?.lua"
local common = require("common")

local SoftenImpact = {}
SoftenImpact.SoftenAfterTop = true
SoftenImpact.AfterTopPercentDistance = 15
SoftenImpact.SoftenBeforeBottom = true
SoftenImpact.BeforeBottomPercentDistance = 15
SoftenImpact.SoftenAfterBottom = true
SoftenImpact.AfterBottomPercentDistance = 15
SoftenImpact.SoftenBeforeTop = true
SoftenImpact.BeforeTopPercentDistance = 15

function SoftenImpact.gui()
	if ofs.Button( "Reset" ) then
		SoftenImpact.SoftenAfterTop = true
		SoftenImpact.SoftenBeforeBottom = true
		SoftenImpact.SoftenAfterBottom = true
		SoftenImpact.SoftenBeforeTop = true
	end

	SoftenImpact.SoftenAfterTop, ATchecked = ofs.Checkbox( "After Top", SoftenImpact.SoftenAfterTop )
	SoftenImpact.AfterTopPercentDistance, ATchanged = ofs.SliderInt("AT", SoftenImpact.AfterTopPercentDistance, 10, 60)
	if ATchanged then
		SoftenImpact.AfterTopPercentDistance = math.min(100 - SoftenImpact.BeforeBottomPercentDistance - 1, SoftenImpact.AfterTopPercentDistance)
	end
	
	SoftenImpact.SoftenBeforeBottom, BBchecked = ofs.Checkbox( "Before Bottom", SoftenImpact.SoftenBeforeBottom )
	SoftenImpact.BeforeBottomPercentDistance, BBchanged = ofs.SliderInt("BB", SoftenImpact.BeforeBottomPercentDistance, 10, 60)
	if BBchanged then
		SoftenImpact.BeforeBottomPercentDistance = math.min(100 - SoftenImpact.AfterTopPercentDistance - 1, SoftenImpact.BeforeBottomPercentDistance)
	end
	
	SoftenImpact.SoftenAfterBottom, ABchecked = ofs.Checkbox( "After Bottom", SoftenImpact.SoftenAfterBottom )
	SoftenImpact.AfterBottomPercentDistance, ABchanged = ofs.SliderInt("AB", SoftenImpact.AfterBottomPercentDistance, 10, 60)
	if ABchanged then
		SoftenImpact.AfterBottomPercentDistance = math.min(100 - SoftenImpact.BeforeTopPercentDistance - 1, SoftenImpact.AfterBottomPercentDistance)
	end
	
	SoftenImpact.SoftenBeforeTop, BTchecked = ofs.Checkbox( "Before Top", SoftenImpact.SoftenBeforeTop )
	SoftenImpact.BeforeTopPercentDistance, BTchanged = ofs.SliderInt("BT", SoftenImpact.BeforeTopPercentDistance, 10, 60)
	if BTchanged then
		SoftenImpact.BeforeTopPercentDistance = math.min(100 - SoftenImpact.AfterBottomPercentDistance - 1, SoftenImpact.BeforeTopPercentDistance)
	end	
	
	if (ATchecked or ATchanged or BBchecked or BBchanged or ABchecked or ABchanged or BTchecked or BTchanged) and ofs.Undo() then
		SoftenImpact.softenImpact()
	end
end

function SoftenImpact.softenImpact()
	local script = ofs.Script(ofs.ActiveIdx())
	local stepSize = common.getFrameStepSize()
	local filter = function(first,second) return first.pos ~= second.pos and second.at - first.at > stepSize * 2 end
    local targetActions = common.getTargetActions(script, filter)
	if targetActions == nil or #targetActions == 0 then
		return
	end
	
	common.StartAddingActions()
		
	for idx, targetAction in ipairs(targetActions) do
		local currentAction = targetAction.first
		local nextAction = targetAction.second
		
		local gap = nextAction.at - currentAction.at
		if currentAction.pos < nextAction.pos then
			if SoftenImpact.SoftenAfterBottom then
				local newAt = currentAction.at + math.max(common.GetClosestFrameTime(gap*(SoftenImpact.AfterBottomPercentDistance/100.0)),stepSize)
				local newPos = (currentAction.pos + (common.getPosAtTime(currentAction, nextAction, newAt)))/2
				common.AddAction(currentAction, nextAction, newAt, newPos)
			end
		
			if SoftenImpact.SoftenBeforeTop then		
				local newAt = nextAction.at - math.max(common.GetClosestFrameTime(gap*(SoftenImpact.BeforeTopPercentDistance/100.0)),stepSize)		
				local newPos = (nextAction.pos + (common.getPosAtTime(currentAction, nextAction, newAt)))/2
				common.AddAction(currentAction, nextAction, newAt, newPos)
			end
		else
			if SoftenImpact.SoftenAfterTop then
				local newAt = currentAction.at + math.max(common.GetClosestFrameTime(gap*(SoftenImpact.AfterTopPercentDistance/100.0)),stepSize)
				local newPos = (currentAction.pos + (common.getPosAtTime(currentAction, nextAction, newAt)))/2
				common.AddAction(currentAction, nextAction, newAt, newPos)
			end
			
			if SoftenImpact.SoftenBeforeBottom then		
				local newAt = nextAction.at - math.max(common.GetClosestFrameTime(gap*(SoftenImpact.BeforeBottomPercentDistance/100.0)),stepSize)		
				local newPos = (nextAction.pos + (common.getPosAtTime(currentAction, nextAction, newAt)))/2
				common.AddAction(currentAction, nextAction, newAt, newPos)
			end
		end	
		
		::continue::
	end

	common.commitNewActions(script)
end

return SoftenImpact