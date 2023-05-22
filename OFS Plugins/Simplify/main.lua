-- OFS plugin to remove actions that don't add much to the motion (based on a set threshold)

local Simplify = {}
Simplify.Difference = 10

function init()
end

function update(delta)
end

function gui()
	if ofs.Button( "Simplify" ) then
		simplify()
	end
	
	ofs.Separator()

	Simplify.Difference, differenceChanged = ofs.SliderInt("Difference(%)", Simplify.Difference, 5, 50)
	if differenceChanged and ofs.Undo() then
		simplify()
	end
end

function simplify()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	
	local actionsToRemoveFound = false
	for idx, action in ipairs(script.actions) do
		if idx == 1 or idx == actionCount or ( script:hasSelection() and not action.selected ) then
			goto continue
		end
		
		local previousAction = script.actions[idx - 1]
		local nextAction = script.actions[idx + 1]
		
		-- Don't remove points that are at the top or bottom of a movement, or points that are meant to hold the actions in place
		if (action.pos < nextAction.pos and action.pos < previousAction.pos) or (action.pos > nextAction.pos and action.pos > previousAction.pos) or 
		   (action.pos == nextAction.pos and action.pos ~= previousAction.pos) or (action.pos ~= nextAction.pos and action.pos == previousAction.pos) then
			goto continue
		end
		
		if (previousAction.pos == action.pos and action.pos == nextAction.pos) or
		   (math.abs(getSpeedBetweenActions(previousAction, action) / getSpeedBetweenActions(action, nextAction) - 1) < Simplify.Difference/100.0) then
			script:markForRemoval(idx)
			actionsToRemoveFound = true
		end
		::continue::
	end

	if actionsToRemoveFound then
		script:removeMarked()
		script:commit()
	end	
end

function getSpeedBetweenActions(first, second)
	local gapInSeconds = second.at - first.at;
	local change = math.abs( second.pos - first.pos )
	return change / gapInSeconds
end