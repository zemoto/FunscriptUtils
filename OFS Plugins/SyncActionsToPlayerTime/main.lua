package.path = package.path .. ";"..os.getenv('APPDATA').."\\OFS\\OFS3_data\\extensions/?.lua"
local common = require("common")

local SyncActionsToPlayerTime = {}
SyncActionsToPlayerTime.TimeTypeSelectedIdx = 1
SyncActionsToPlayerTime.BPM = 100
SyncActionsToPlayerTime.Offset = 0.0
SyncActionsToPlayerTime.SelectChanges = false

function binding.syncActionsToPlayerTime()
	syncActionsToPlayerTime()
end

function init()
end

function update(delta)
end

function gui()
	SyncActionsToPlayerTime.TimeTypeSelectedIdx, changed = ofs.Combo( "", SyncActionsToPlayerTime.TimeTypeSelectedIdx, { "Frame", "Tempo" } )
	
	if SyncActionsToPlayerTime.TimeTypeSelectedIdx == 1 then -- Frame
	elseif SyncActionsToPlayerTime.TimeTypeSelectedIdx == 2 then --Tempo
		SyncActionsToPlayerTime.BPM, changed = ofs.InputInt( "BPM", SyncActionsToPlayerTime.BPM, 1 )
		SyncActionsToPlayerTime.Offset, changed = ofs.Drag( "Offset", SyncActionsToPlayerTime.Offset, 0.001 )
	end
	
	SyncActionsToPlayerTime.SelectChanges, changed = ofs.Checkbox( "Select changes", SyncActionsToPlayerTime.SelectChanges )
end

function syncActionsToPlayerTime()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	if not script:hasSelection() then
		return
	end
	
	local offset = SyncActionsToPlayerTime.Offset
	local stepSize
	if SyncActionsToPlayerTime.TimeTypeSelectedIdx == 1 then -- Frame
		stepSize = common.getFrameStepSize()
		offset = 0
	elseif SyncActionsToPlayerTime.TimeTypeSelectedIdx == 2 then --Tempo
		stepSize = 60 / ( SyncActionsToPlayerTime.BPM * 8 )
	end
	
	local videoDuration = player.Duration()
	local correctionThreshold = 0.0005
	local changesToBeMade = {}
	for i=1,actionCount do
		local prevAction = nil
		local action = script.actions[i]
		local nextAction = nil
		local originallySelected = action.selected
		if SyncActionsToPlayerTime.SelectChanges then
			action.selected = false
		end
		
		if i > 1 then
			prevAction = script.actions[i-1]
		end
		
		if i < actionCount then
			nextAction = script.actions[i+1]
		end
		
		local haveRoomBehind = ((prevAction == nil and action.at > stepSize) or (prevAction ~= nil and action.at - prevAction.at > stepSize))
		local haveRoomAhead = ((nextAction == nil and videoDuration - action.at > stepSize) or (nextAction ~= nil and nextAction.at - action.at > stepSize))
		if not originallySelected or (not haveRoomBehind and not haveRoomAhead) then
			goto continue
		end
	
		local offStepDistance = ( action.at - offset ) % stepSize
		if offStepDistance <= correctionThreshold or offStepDistance >= stepSize - correctionThreshold then
			goto continue
		end
				
		local newAt = nil
		if offStepDistance >= stepSize / 2 then
			if not haveRoomAhead then
				goto continue
			end
			newAt = action.at - offStepDistance + stepSize
		else
			if not haveRoomBehind then
				goto continue
			end
			newAt = action.at - offStepDistance
		end
		
		table.insert(changesToBeMade, {index = i, newAt = newAt})
		
		::continue::		
	end
	
	for j, changeToBeMade in ipairs(changesToBeMade) do
		script.actions[changeToBeMade.index].at = changeToBeMade.newAt
		if SyncActionsToPlayerTime.SelectChanges then
			script.actions[changeToBeMade.index].selected = true
		end
	end
	
	if #changesToBeMade > 0 then
		script:commit()
	end
end
