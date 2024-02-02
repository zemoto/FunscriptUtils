function center(min,max)
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	
	local desiredCenter = ( min + max ) / 2
	
	local actionsToAdjust = {}
	local lastAdjustment = -1
	
	local lastIdx = 0
	for i=3,actionCount-2 do
		if i <= lastIdx then
			goto continue
		end
	
		local prevPrevAction = script.actions[i-2]
		local prevAction = script.actions[i-1]
		local currentAction = script.actions[i]
		local nextAction = script.actions[i+1]
		local nextNextAction = script.actions[i+2]
		
		if not currentAction.selected or not nextAction.selected then
			goto continue
		end
		
		local prevIsHold = prevPrevAction.pos == prevAction.pos
		local isHold = prevAction.pos == currentAction.pos
		local nextIsHold = currentAction.pos == nextAction.pos
		local isTopHold = isHold and prevAction.pos > prevPrevAction.pos
		local isBottomHold = isHold and prevAction.pos < prevPrevAction.pos
		local isTop = (prevAction.pos < currentAction.pos) or isTopHold
		local isBottom = (prevAction.pos > currentAction.pos) or isBottomHold
		local isStart = not prevAction.selected
		local nextIsEnd = not nextNextAction.selected
		local nextNextIsHold = nextAction.pos == nextNextAction.pos
		
		local newPos = currentAction.pos
		
		if isStart then		
			if isTop and currentAction.pos < max then
				newPos = max
			elseif isBottom and currentAction.pos > min then
				newPos = min
			end
			if isHold and newPos ~= currentAction.pos then
				table.insert(actionsToAdjust, {idx=i-1, pos=newPos})
			end
		elseif nextIsEnd or nextNextIsHold then
			if (nextAction.pos > currentAction.pos or (nextAction.pos == currentAction.pos and isTop)) and nextAction.pos < max then
				local adjustment = max - nextAction.pos
				newPos = currentAction.pos + adjustment
				if nextIsEnd then
					table.insert(actionsToAdjust, {idx=i+1, pos=max})
					if nextNextIsHold then
						table.insert(actionsToAdjust, {idx=i+2, pos=max})
					end
				end
			elseif (nextAction.pos < currentAction.pos or (nextAction.pos == currentAction.pos and isBottom)) and nextAction.pos > min then
				local adjustment = nextAction.pos - min
				newPos = currentAction.pos - adjustment
				if nextIsEnd then
					table.insert(actionsToAdjust, {idx=i+1, pos=min})
					if nextNextIsHold then
						table.insert(actionsToAdjust, {idx=i+2, pos=min})
					end
				end
			end			
		elseif prevIsHold or isHold or nextIsHold then
			newPos = currentAction.pos + lastAdjustment
		else
			local currentCenter = (currentAction.pos + nextAction.pos)/2
			if currentCenter ~= desiredCenter then
				newPos = currentAction.pos + (desiredCenter - currentCenter)
			end
		end
		
		lastAdjustment = (newPos - currentAction.pos)
		if newPos ~= currentAction.pos then
			table.insert(actionsToAdjust, {idx=i, pos=newPos})
		end

		::continue::
	end
	
	for idx, adjustmentInfo in ipairs(actionsToAdjust) do
		script.actions[adjustmentInfo.idx].pos = adjustmentInfo.pos
	end
	
	script:sort()
	script:commit()
end

return { center = center }

--[[ Version that keeps center actions centered
function center(min,max)
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	
	local desiredCenter = ( min + max ) / 2
	
	local actionsToAdjust = {}
	local lastAdjustment = -1
	
	local lastIdx = 0
	for i=3,actionCount-2 do
		if i <= lastIdx then
			goto continue
		end
	
		local prevPrevAction = script.actions[i-2]
		local prevAction = script.actions[i-1]
		local currentAction = script.actions[i]
		local nextAction = script.actions[i+1]
		local nextNextAction = script.actions[i+2]
		
		if not currentAction.selected then
			goto continue
		end
				
		local isHold = prevAction.pos == currentAction.pos
		local nextIsHold = currentAction.pos == nextAction.pos
		local isTopHold = isHold and prevAction.pos > prevPrevAction.pos
		local isBottomHold = isHold and prevAction.pos < prevPrevAction.pos
		local isTop = (prevAction.pos < currentAction.pos) or isTopHold
		local isBottom = (prevAction.pos > currentAction.pos) or isBottomHold
		local isStart = not prevAction.selected
		local nextIsEnd = not nextNextAction.selected
		
		if isStart then		
			if isTop and currentAction.pos < max then
				newPos = max
			elseif isBottom and currentAction.pos > min then
				newPos = min
			end
		elseif nextIsEnd then
			if (nextAction.pos > currentAction.pos or (nextAction.pos == currentAction.pos and isTop)) and nextAction.pos < max then
				local adjustment = max - nextAction.pos
				table.insert(actionsToAdjust, {idx=i+1, pos=max})
				table.insert(actionsToAdjust, {idx=i, pos=currentAction.pos + adjustment})
				if nextNextAction.pos == nextAction.pos then
					table.insert(actionsToAdjust, {idx=i+2, pos=max})
				end
			elseif (nextAction.pos < currentAction.pos or (nextAction.pos == currentAction.pos and isBottom)) and nextAction.pos > min then
				local adjustment = nextAction.pos - min
				table.insert(actionsToAdjust, {idx=i+1, pos=min})
				table.insert(actionsToAdjust, {idx=i, pos=currentAction.pos - adjustment})
				if nextNextAction.pos == nextAction.pos then
					table.insert(actionsToAdjust, {idx=i+2, pos=min})
				end
			end
		
			break			
		elseif isHold or nextIsHold then
			newPos = currentAction.pos + lastAdjustment
		else
			local currentCenter = (currentAction.pos + nextAction.pos)/2
			if currentCenter ~= desiredCenter then
				newPos = currentAction.pos + (desiredCenter - currentCenter)
			end
		end
		
		if newPos ~= currentAction.pos then
			lastAdjustment = (newPos - currentAction.pos)
			table.insert(actionsToAdjust, {idx=i, pos=newPos})
			if isHold then
				table.insert(actionsToAdjust, {idx=i-1, pos=newPos})
			end
		end

		::continue::
	end
	
	for idx, adjustmentInfo in ipairs(actionsToAdjust) do
		script.actions[adjustmentInfo.idx].pos = adjustmentInfo.pos
	end
	
	script:sort()
	script:commit()
end
--]]