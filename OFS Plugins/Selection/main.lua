function init()
end

function update(delta)
end

function gui()
	if ofs.Button( "Select Down Action" ) then
		selectDownAction()
	end
end

function selectDownAction()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	
	if not script:hasSelection() then
		return
	end
	
	local actionsToSelect = {}
	for i=2,actionCount-1 do
		local previousAction = script.actions[i-1]
		local action = script.actions[i]
		local nextAction = script.actions[i+1]
		
		if action.selected and previousAction.pos > action.pos and action.pos < nextAction.pos then
			table.insert(actionsToSelect, i-1)
			table.insert(actionsToSelect, i)
		end
		
		if i == 2 then
			previousAction.selected = false;
		end
		action.selected = false;
		if i == actionCount then
			nextAction.selected = false;
		end	
	end
	
	for i, selectIdx in ipairs(actionsToSelect) do
		script.actions[selectIdx].selected = true
	end
	
	script:commit()
end
