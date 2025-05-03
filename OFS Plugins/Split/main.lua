function binding.split()
	split()
end

function init()
end

function update(delta)
end

function gui()
end

function split()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	if not script:hasSelection() then
		return
	end
	
	local newActions = {}
	
	for i=1,actionCount-1 do
		local action = script.actions[i]
		local nextAction = script.actions[i+1]
		if not action.selected or not nextAction.selected then
			goto continue
		end
		
		local slope = (nextAction.pos - action.pos) / (nextAction.at - action.at)
		local intercept = action.pos - (slope * action.at)
		
		local time = (nextAction.at + action.at) / 2
		local pos = (slope * time) + intercept
		
		table.insert(newActions, {at=time, pos=pos})
		
		::continue::
	end
	
	for idx, newAction in ipairs(newActions) do
		script.actions:add(Action.new(newAction.at, newAction.pos, true))
	end
	
	script:sort()
	script:commit()
end
