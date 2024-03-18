local ChangingMax = {}
ChangingMax.StartMax = 50
ChangingMax.EndMax = 50

function ChangingMax.changingMax()
	local script = ofs.Script(ofs.ActiveIdx())
	
	if not script:hasSelection() then
		return
	end
	
	local startIdx = -1
	local endIdx = -1
	for idx, action in ipairs(script.actions) do
		if action.selected and startIdx == -1 then
			startIdx = idx
		elseif action.selected and startIdx ~= -1 then
			endIdx = idx
		elseif not action.selected and startIdx ~= -1 and endIdx ~= -1 then
			break
		elseif not action.selected and startIdx ~= -1 and endIdx == -1 then
			return
		end
	end
	
	if startIdx == -1 or endIdx == -1 then
		return
	end
	
	local startAction = script.actions[startIdx]
	local endAction = script.actions[endIdx]	
	local slope = (ChangingMax.EndMax - ChangingMax.StartMax) / (endAction.at - startAction.at)
	local intercept = ChangingMax.StartMax - (slope * startAction.at)
	
	local localMax = 0
	for i=startIdx,endIdx do
		local action = script.actions[i]
		localMax = math.max(localMax, action.pos)
	end
	
	local changesMade = false
	for i=startIdx,endIdx do
		local action = script.actions[i]
		local maxAtTime = math.floor((slope * action.at) + intercept)
		local adjustment = maxAtTime/localMax
		
		local newPos = math.min(action.pos * adjustment, 100)
		if action.pos ~= newPos then
			action.pos = newPos
			changesMade = true
		end
	end
	
	if changesMade then
		script:commit()
	end
end

return ChangingMax