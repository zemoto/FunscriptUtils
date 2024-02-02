local function getPosAtTime(first, second, time)
	local gapInSeconds = second.at - first.at;
	local change = second.pos - first.pos
	local speed = change / gapInSeconds
	
	local relativeTime = time - first.at
	return first.pos + (speed * relativeTime)	
end

local function getSpeed(firstTime, firstPos, secondTime, secondPos)
	local gapInSeconds = secondTime - firstTime;
	local change = math.abs(secondPos - firstPos)
	return change / gapInSeconds
end

local SoftenImpact = {}
SoftenImpact.PercentDistance = 15
SoftenImpact.SoftenBeforeTop = false
SoftenImpact.SoftenAfterTop = false
SoftenImpact.SoftenBeforeBottom = false
SoftenImpact.SoftenAfterBottom = false
SoftenImpact.SoftenBeforeMiddle = false
SoftenImpact.SoftenAfterMiddle = false

function SoftenImpact.softenImpact(speedLimit)
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	
	if not script:hasSelection() then
		return
	end
	
	local newActions = {}

	local changesMade = false
	for i=1,actionCount do
		local prevAction = nil
		local nextAction = nil
		local currentAction = script.actions[i]
		
		if not currentAction.selected then
			goto continue
		end
		
		if i > 1 then
			prevAction = script.actions[i-1]
		end
		
		if i < actionCount then
			nextAction = script.actions[i+1]
		end
		
		local isTop = ((prevAction ~= nil and currentAction.pos >= prevAction.pos) or prevAction == nil) and ((nextAction ~= nil and currentAction.pos >= nextAction.pos) or nextAction == nil)
		local isBottom = ((prevAction ~= nil and currentAction.pos <= prevAction.pos) or prevAction == nil) and ((nextAction ~= nil and currentAction.pos <= nextAction.pos) or nextAction == nil)
		local isMiddle = not isTop and not isBottom
		
		local shouldSoftenBefore = (isTop and SoftenImpact.SoftenBeforeTop) or (isBottom and SoftenImpact.SoftenBeforeBottom) or (isMiddle and SoftenImpact.SoftenBeforeMiddle)
		if shouldSoftenBefore and prevAction ~= nil and prevAction.selected and prevAction.pos ~= currentAction.pos and currentAction.at - prevAction.at > 0.09 then		
			local prevImpactTime = math.max((currentAction.at - prevAction.at)*(SoftenImpact.PercentDistance/100.0),0.03333)
			
			-- When you go from non-0 to 0, it does not bottom out the device but 0 to 0 does
			-- Therefore, going from non-0 to 0 to 0 creates the desired slow-down effect
			local prevImpactPos = 0
			if not isBottom then
				prevImpactPos = (currentAction.pos + (getPosAtTime(prevAction, currentAction, currentAction.at-prevImpactTime)))/2
			end
			table.insert(newActions, {at=currentAction.at-prevImpactTime, pos=prevImpactPos})
			changesMade = true
		end
		
		local shouldSoftenAfter = (isTop and SoftenImpact.SoftenAfterTop) or (isBottom and SoftenImpact.SoftenAfterBottom) or (isMiddle and SoftenImpact.SoftenAfterMiddle)
		if shouldSoftenAfter and nextAction ~= nil and nextAction.selected and nextAction.pos ~= currentAction.pos and nextAction.at - currentAction.at > 0.09 then
			local nextImpactTime = math.max((nextAction.at - currentAction.at)*(SoftenImpact.PercentDistance/100.0),0.03333)
			local nextImpactPos = (currentAction.pos + (getPosAtTime(currentAction, nextAction, currentAction.at+nextImpactTime)))/2
			table.insert(newActions, {at=currentAction.at+nextImpactTime, pos=nextImpactPos})
			changesMade = true
		end
		
		::continue::
	end

	if changesMade then
		for idx, newAction in ipairs(newActions) do
			local newAction = Action.new(newAction.at, newAction.pos);
			newAction.selected = true
			script.actions:add(newAction)
		end
		
		script:sort()
		script:commit()
	end
end

return SoftenImpact