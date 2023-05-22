local function getSpeedBetweenActions(first, second)
	local gapInSeconds = second.at - first.at;
	local change = math.abs(second.pos - first.pos)
	return change / gapInSeconds
end

function vibrate(speed,timeBetweenVibrations)
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	
	if not script:hasSelection() then
		return
	end
   
    local maxSpeedThreshold = speed / 2
	local vibrationDistance = math.floor((speed * timeBetweenVibrations) / 2 + 0.5)
    local newActions = {}
	local changesMade = false
	for i=1,actionCount-1 do
		local start = script.actions[i]
		local endAction = script.actions[i+1]
		local speedBetweenStartAndEnd = getSpeedBetweenActions(start, endAction)
		if not start.selected or not endAction.selected or speedBetweenStartAndEnd > maxSpeedThreshold then 
			goto continue
		end
		
		local timePoints = {}
		local currentTime = start.at + timeBetweenVibrations		
		while currentTime < endAction.at - timeBetweenVibrations do
			table.insert(timePoints, currentTime)
			currentTime = currentTime + timeBetweenVibrations
		end
		
		if #timePoints == 0 then
			goto continue
		end
		
		-- Only want an odd number of points if the vibration will clip near the bounds
		local vibrationWillClip = (start.pos < vibrationDistance and endAction.pos < vibrationDistance) or (start.pos > 100 - vibrationDistance and endAction.pos > 100 - vibrationDistance)
		if not vibrationWillClip and #timePoints % 2 == 1 then
			table.remove(timePoints, numberOfTimePoints)
		end
		
		-- Ensure the vibration don't clip
		local startPos = start.pos
		local endPos = endAction.pos
		if vibrationWillClip then
			startPos = math.max(math.min(start.pos, 100 - vibrationDistance), vibrationDistance)
			endPos = math.max(math.min(endAction.pos, 100 - vibrationDistance), vibrationDistance)
		end
		
		local slope = (endPos - startPos) / (endAction.at - start.at)
		local intercept = startPos - (slope * start.at)
		
		local vibrationActions = {}
		local addingBottom = slope < 0 or (slope == 0 and start.pos > 100 - vibrationDistance)
		for i, time in ipairs(timePoints) do
			local centerLineAtTime = (slope * time) + intercept
			local position = 0
			if addingBottom then
				position = centerLineAtTime - vibrationDistance
			else
				position = centerLineAtTime + vibrationDistance
			end
			
			position = clamp(position, 0, 100)
			
			table.insert(vibrationActions, {at=time, pos=position})
			
			addingBottom = not addingBottom;
		end
		
		local numberOfNewActions = #vibrationActions
		if numberOfNewActions > 0 then
			-- Position the actions equal distances from start and end
			local distanceFromStart = math.abs(vibrationActions[1].pos - start.pos)
			local distanceFromEnd = math.abs(vibrationActions[numberOfNewActions].pos - endAction.pos )
			local adjustment = 0
			if slope < 0 then
				adjustment = (distanceFromStart - distanceFromEnd) / 2
			elseif slope > 0 then
				adjustment = (distanceFromEnd - distanceFromStart) / 2
			end
		
			-- Evenly distribute the actions
			local timeBetweenActions = (endAction.at - start.at) / (numberOfNewActions + 1);
			currentTime = start.at + timeBetweenActions;
			for i, newAction in ipairs(vibrationActions) do
				newAction.at = currentTime;
				currentTime = currentTime + timeBetweenActions;
				
				newAction.pos = newAction.pos + adjustment;
			end
			
			-- Ensure the vibration is below the target speed
			for i, newAction in ipairs(vibrationActions) do
				local prevAction = start
				if i > 1 then
					prevAction = vibrationActions[i-1]
				end
				
				local maxDistance = math.floor(speed * (newAction.at - prevAction.at))
				if math.abs(prevAction.pos - newAction.pos) > maxDistance then				
					if newAction.pos < prevAction.pos then
						newAction.pos = prevAction.pos - maxDistance
					else
						newAction.pos = prevAction.pos + maxDistance
					end
				end
				
				if i == numberOfNewActions then
					maxDistance = math.floor(speed * (endAction.at - newAction.at))
					if math.abs(endAction.pos - newAction.pos) > maxDistance then
						if newAction.pos < endAction.pos then
							newAction.pos = endAction.pos - maxDistance
						else
							newAction.pos = endAction.pos + maxDistance
						end
					end
				end
			end
			
			for i, newAction in ipairs(vibrationActions) do
				table.insert(newActions, newAction)
			end
		end
		
		changesMade = changesMade or numberOfNewActions > 0
		::continue::
	end
	
	if changesMade then
		for idx, newAction in ipairs(newActions) do
			script.actions:add(Action.new(newAction.at, newAction.pos))
		end
		
		script:sort()
	
		for idx, action in ipairs(script.actions) do
			action.selected = false
		end
		script:commit()
	end
end

return { vibrate = vibrate }