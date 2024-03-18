function init()
end

function update(delta)
end

function gui()
	if ofs.Button( "Clean Vibration" ) then
		cleanVibration()
	end
	
	ofs.SameLine()
	
	if ofs.Button( "Delete Intermediary" ) then
		deleteIntermediary()
	end
	
	if ofs.Button( "Delete MidPoints" ) then
		deleteMidPoints()
	end
	
	ofs.SameLine()
	
	if ofs.Button( "Clean Redundants" ) then
		cleanRedundants()
	end
	
	if ofs.Button( "Delete Pre-Holds" ) then
		deletePreHolds()
	end
	
	ofs.SameLine()
	
	if ofs.Button( "Delete Post-Holds" ) then
		deletePostHolds()
	end
	
	if ofs.Button( "Clean Ignored MidPoints" ) then
		deleteIgnoredMidPoints()
	end
end

function getSpeedBetweenActions(first, second)
	local gapInSeconds = second.at - first.at;
	local change = math.abs( second.pos - first.pos )
	return change / gapInSeconds
end

function cleanVibration()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	
	if not script:hasSelection() then
		return
	end
	
	local vibrationDistanceThreshold = 0.072
	local actionsToRemoveFound = false
	local firstIdx = 1
	local lastIdx = 1
	while lastIdx < actionCount do
		local startOfVibration = -1
		local endOfVibration = -1
		local localPeak = -1
		local localValley = 101
		for i=lastIdx,actionCount-2 do
			local action = script.actions[i]
			local nextAction = script.actions[i+1]
			local nextNextAction = script.actions[i+2]

			if not action.selected then
				goto cont2
			end
			
			if not nextAction.selected then
				if startOfVibration ~= -1 then
					endOfVibration = i
				end
				break
			end
			
			localPeak = math.max(localPeak, action.pos)
			localValley = math.min(localValley, action.pos)
					
			if startOfVibration == -1 then
				if nextAction.at - action.at <= vibrationDistanceThreshold then
					startOfVibration = i
					
					if i == actionCount - 1 then
						endOfVibration = i+1
						break
					end
				end
			else		
				if nextNextAction.selected then
					if (action.pos == localPeak and nextNextAction.pos <= action.pos) or (action.pos == localValley and nextNextAction.pos >= action.pos) then
						endOfVibration = i
					break
					end
				end
				
				if nextAction.at - action.at <= vibrationDistanceThreshold then				
					if i == actionCount - 1 then
						endOfVibration = i+1
					end
				else
					endOfVibration = i
				end
			end
			
			::cont2::
			
			lastIdx = i+1
			if endOfVibration ~= -1 then
				break
			end
			
			if startOfVibration ~= -1 and i == actionCount - 2 then
				endOfVibration = i+1
			end
		end
		
		if startOfVibration == -1 or endOfVibration == -1 then
			break
		end

		for i=startOfVibration+1,endOfVibration-1 do
			script:markForRemoval(i)
			actionsToRemoveFound = true
		end
		
		if lastIdx >= actionCount then
			break
		end
	end

	if actionsToRemoveFound then
		script:removeMarked()
		script:commit()
	end	
end

function deleteIntermediary()
	local script = ofs.Script(ofs.ActiveIdx())
	
	if not script:hasSelection() then
		return
	end
	
	local first = -1
	local last = -1
	for idx, action in ipairs(script.actions) do
		if action.selected then
			if first == -1 then
				first = idx
			end
			last = idx
		elseif first ~= -1 then
			break
		end
	end
	
	if last - first > 1 then
		for i=first+1,last-1 do	
			script:markForRemoval(i)
		end
		script:removeMarked()
		script:commit()
	end	
end

function deleteMidPoints()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	
	if not script:hasSelection() then
		return
	end
	
	local actionsToRemoveFound = false
	for i=2,actionCount-1 do
		local prevAction = script.actions[i-1]
		local currentAction = script.actions[i]
		local nextAction = script.actions[i+1]
	
		if not currentAction.selected then
			goto continue
		end
		
		local isBottom = currentAction.pos < nextAction.pos and currentAction.pos < prevAction.pos
		local isTop = currentAction.pos > nextAction.pos and currentAction.pos > prevAction.pos
		local isHold = currentAction.pos == nextAction.pos or currentAction.pos == prevAction.pos
		local isMid = not isBottom and not isTop and not isHold
		
		if isMid then
			script:markForRemoval(i)
			actionsToRemoveFound = true
		end
		
		::continue::
	end

	if actionsToRemoveFound then
		script:removeMarked()
		script:commit()
	end	
end

function deletePreHolds()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	
	if not script:hasSelection() then
		return
	end
	
	local actionsToRemoveFound = false	
	for i=1,actionCount-1 do
		local currentAction = script.actions[i]
		local nextAction = script.actions[i+1]
		
		if currentAction.selected and currentAction.pos == nextAction.pos then
			script:markForRemoval(i)
			actionsToRemoveFound = true
		end
	end
	
	if actionsToRemoveFound then
		script:removeMarked()
		script:commit()
	end	
end

function deletePostHolds()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	
	if not script:hasSelection() then
		return
	end
	
	local actionsToRemoveFound = false	
	for i=2,actionCount do
		local prevAction = script.actions[i-1]
		local currentAction = script.actions[i]
		
		if currentAction.selected and currentAction.pos == prevAction.pos then
			script:markForRemoval(i)
			actionsToRemoveFound = true
		end
	end
	
	if actionsToRemoveFound then
		script:removeMarked()
		script:commit()
	end	
end

function cleanRedundants()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	
	local actionsToRemoveFound = false	
	for i=2,actionCount do
		local prevAction = script.actions[i-1]
		local currentAction = script.actions[i]
		
		if currentAction.at - prevAction.at < 0.019 then
			script:markForRemoval(i)
			actionsToRemoveFound = true
		end
	end
	
	if actionsToRemoveFound then
		script:removeMarked()
		script:commit()
	end	
end

function deleteIgnoredMidPoints()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	
	local actionsToRemoveFound = false
	for i=2,actionCount-1 do
		local prevAction = script.actions[i-1]
		local currentAction = script.actions[i]
		local nextAction = script.actions[i+1]
	
		local isBottom = currentAction.pos < nextAction.pos and currentAction.pos < prevAction.pos
		local isTop = currentAction.pos > nextAction.pos and currentAction.pos > prevAction.pos
		local isHold = currentAction.pos == nextAction.pos or currentAction.pos == prevAction.pos
		local isMid = not isBottom and not isTop and not isHold
		
		if isMid and getSpeedBetweenActions(prevAction,currentAction) > 364 then
			script:markForRemoval(i)
			actionsToRemoveFound = true
		end
		
		::continue::
	end

	if actionsToRemoveFound then
		script:removeMarked()
		script:commit()
	end	
end