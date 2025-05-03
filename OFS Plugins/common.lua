local Common = {}

local function areSameTime(first, second)
	return math.abs(first-second) < 0.001
end

function Common.getFrameStepSize()
	return 1 / player.FPS()
end

function Common.getPosAtTime(first, second, time)
	local gapInSeconds = second.at - first.at;
	local change = second.pos - first.pos
	local speed = change / gapInSeconds
	
	local relativeTime = time - first.at
	return first.pos + (speed * relativeTime)	
end

function Common.GetClosestFrameTime(time)
	local stepSize = Common.getFrameStepSize()
	local dif = time % stepSize
	if dif >= stepSize / 2 then
		return time - dif + stepSize
	else
		return time - dif
	end
end

function Common.getTargetActions(script, filterFunc)
	if not script:hasSelection() then
		local currentTime = player.CurrentTime()
		startAction = script:closestActionBefore(currentTime + 0.015)
		endAction = script:closestActionAfter(currentTime + 0.015)
		if startAction ~= nil and endAction ~= nil and (filterFunc == nil or filterFunc(startAction, endAction)) then
			return {{first=startAction, second=endAction}}
		end
	else  
		local actionCount = #script.actions
		local actionPairs = {}
		for i=1,actionCount-1 do
			local startAction = script.actions[i]
			local endAction = script.actions[i+1]
			if startAction.selected and endAction.selected and (filterFunc == nil or filterFunc(startAction, endAction)) then 
				table.insert(actionPairs, {first=startAction, second=endAction})
			end
		end
		
		return actionPairs
	end
	
	return nil
end

function Common.commitNewActions(script)
	local newActions = Common.EndAddingActions()
	if #newActions > 0 then
		for idx, newAction in ipairs(newActions) do
			script.actions:add(Action.new(newAction.at, newAction.pos))
		end
		
		script:sort()
	
		for idx, action in ipairs(script.actions) do
			action.selected = false
		end
		script:commit()
		return true
	end
	
	return false
end

local newActions = {}

function Common.StartAddingActions()
	newActions = {}
end

function Common.AddAction(previousAction, nextAction, newAt, newPos)
	if areSameTime(previousAction.at, newAt) or areSameTime(nextAction.at, newAt) then
		return
	end
	
	for k, v in pairs(newActions) do
		if areSameTime(v.at, newAt) then
			return
		end
	end
	
	table.insert(newActions, {at=newAt, pos=newPos})
end

function Common.EndAddingActions()
	return newActions
end

return Common