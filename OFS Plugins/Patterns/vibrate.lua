local Vibrate = {}
Vibrate.Intensity = 100
Vibrate.Speed = 454
Vibrate.MSBetweenVibrations = 35
Vibrate.EndIsMiddlePoint = false

function Vibrate.vibrate()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	local usePlayhead = false
	
	if not script:hasSelection() then
		usePlayhead = true
		actionCount = 2
	end
   
    local timeBetweenVibrations = Vibrate.MSBetweenVibrations / 1000
    local newActions = {}
	local changesMade = false
	for i=1,actionCount-1 do	
		local start = {}
		local endAction = {} 
		if usePlayhead then
			local currentTime = player.CurrentTime()
			start = script:closestAction(currentTime)
			endAction = script:closestActionAfter(currentTime + 0.015)
			if start == nil or endAction == nil then
				return
			end
		else
			start = script.actions[i]
			endAction = script.actions[i+1]
			if not start.selected or not endAction.selected then 
				goto continue
			end
		end
		
		local numTimePoints = math.floor((endAction.at - start.at) / timeBetweenVibrations)
		if numTimePoints == 0 then
			goto continue
		end
		
		local timePoints = {}
		local timeBetweenActions = (endAction.at - start.at) / (numTimePoints + 1);
		currentTime = start.at + timeBetweenActions;
		for j=1,numTimePoints do
			table.insert(timePoints, currentTime)
			currentTime = currentTime + timeBetweenActions;
		end
		
		local vibrationDistance = math.max(math.floor(Vibrate.Speed * timeBetweenActions / 2),3)
		
		-- Ensure the vibration doesn't clip
		local startPos = start.pos
		local endPos = endAction.pos		
		local vibrationClippingTop = start.pos > 100 - vibrationDistance and endAction.pos > 100 - vibrationDistance
		local vibrationClippingBottom = start.pos < vibrationDistance and endAction.pos < vibrationDistance
		if vibrationClippingTop or vibrationClippingBottom then
			startPos = math.max(math.min(start.pos, 100 - vibrationDistance), vibrationDistance)
			endPos = math.max(math.min(endAction.pos, 100 - vibrationDistance), vibrationDistance)
		elseif start.pos == endAction.pos then
			startPos = startPos + vibrationDistance
			endPos = endPos + vibrationDistance
		end
		
		local slope = (endPos - startPos) / (endAction.at - start.at)
		local intercept = startPos - (slope * start.at)
		
		local vibrationActions = {}
		local addingBottom = slope < 0 or (slope == 0 and start.pos > 100 - vibrationDistance)
		if Vibrate.Invert then
			addingBottom = not addingBottom
		end
		
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
		for k, newAction in ipairs(vibrationActions) do
			table.insert(newActions, newAction)
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

return Vibrate