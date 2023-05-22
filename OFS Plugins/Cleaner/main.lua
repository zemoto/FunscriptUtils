function init()
end

function update(delta)
end

function gui()
	if ofs.Button( "Delete MidPoints" ) then
		deletePoints()
	end
	
	ofs.SameLine()
	
	if ofs.Button( "Delete Non-Peaks" ) then
		deleteNonPeak()
	end
end

function deletePoints()
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

function deleteNonPeak()
	local script = ofs.Script(ofs.ActiveIdx())
	
	if not script:hasSelection() then
		return
	end
	
	local peak = 0
	local valley = 0
	for idx, action in ipairs(script.actions) do
		if action.selected then
			peak = math.max(peak,action.pos)
			valley = math.min(valley,action.pos)
		end
	end
	
	local actionsToRemoveFound = false
	for idx, action in ipairs(script.actions) do	
		if action.selected and action.pos ~= peak and action.pos ~= valley then
			script:markForRemoval(idx)
			actionsToRemoveFound = true
		end
	end

	if actionsToRemoveFound then
		script:removeMarked()
		script:commit()
	end	
end