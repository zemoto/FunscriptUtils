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

function softenImpact(speedLimit, percentDistance)
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
		
		if prevAction ~= nil and prevAction.selected and prevAction.pos < currentAction.pos and currentAction.at - prevAction.at > 0.09 then
			local prevImpactTime = math.max((currentAction.at - prevAction.at)*(percentDistance/100.0),0.045)
			local prevImpactPos = (currentAction.pos + (getPosAtTime(prevAction, currentAction, currentAction.at-prevImpactTime)))/2
			
			-- Need to check if adding an impact action before the current puts us over the speed limit. Need to check against impact action added after previous action. 
			-- Impact actions added after an action are fine, but actions added before that go over the speed limit are useless.
			local posOfImpactActionAddedAfterPrev = (prevAction.pos + (getPosAtTime(prevAction,currentAction,prevAction.at+prevImpactTime)))/2
			
			if getSpeed(prevAction.at+prevImpactTime, posOfImpactActionAddedAfterPrev, currentAction.at-prevImpactTime, prevImpactPos) < speedLimit then		
				table.insert(newActions, {at=currentAction.at-prevImpactTime, pos=prevImpactPos})
				changesMade = true
			end
		end
		
		if nextAction ~= nil and nextAction.selected and nextAction.pos ~= currentAction.pos and nextAction.at - currentAction.at > 0.09 then
			local nextImpactTime = math.max((nextAction.at - currentAction.at)*(percentDistance/100.0),0.045)
			local nextImpactPos = (currentAction.pos + (getPosAtTime(currentAction, nextAction, currentAction.at+nextImpactTime)))/2		
			table.insert(newActions, {at=currentAction.at+nextImpactTime, pos=nextImpactPos})
			changesMade = true
		end
		
		::continue::
	end

	if changesMade then
		for idx, newAction in ipairs(newActions) do
			script.actions:add(Action.new(newAction.at, newAction.pos))
		end
		
		script:sort()
		script:commit()
	end
end

return { softenImpact = softenImpact }