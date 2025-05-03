package.path = package.path .. ";"..os.getenv('APPDATA').."\\OFS\\OFS3_data\\extensions/?.lua"
local common = require("common")
local vibrate = require("vibrate")

local Preset = {}
Preset.SelectedPresetIdx = 1

function Preset.gui()
	Preset.SelectedPresetIdx, changed = ofs.Combo( "Type", Preset.SelectedPresetIdx, { "Grow and Shrink", "Keggle", "Pulsing Vibration" } )
end

function Preset.applySelectedPreset()
	if Preset.SelectedPresetIdx == 1 then
		growAndShrink()
	elseif Preset.SelectedPresetIdx == 2 then
		keggle()
	elseif Preset.SelectedPresetIdx == 3 then
		pulsingVibration()
	end
end

function growAndShrink()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	local currentTime = player.CurrentTime()
	
	local startTime = currentTime - 0.21069
	
	for i=1,actionCount do
		local actionTime = script.actions[i].at
		if actionTime > startTime + 0.5 then
			break
		elseif actionTime >= currentTime - 0.22 and actionTime <= currentTime + 0.22 then
			return
		end
	end
	
	script.actions:add(Action.new(startTime, 0))
	script.actions:add(Action.new(startTime + 0.04218, 19))
	script.actions:add(Action.new(startTime + 0.08429, 13))
	script.actions:add(Action.new(startTime + 0.12646, 32))
	script.actions:add(Action.new(startTime + 0.16858, 26))
	script.actions:add(Action.new(startTime + 0.21069, 45))
	script.actions:add(Action.new(startTime + 0.25287, 26))
	script.actions:add(Action.new(startTime + 0.29498, 32))
	script.actions:add(Action.new(startTime + 0.33716, 13))
	script.actions:add(Action.new(startTime + 0.37927, 19))
	script.actions:add(Action.new(startTime + 0.42145, 0))	
	
	script:sort()
	script:commit()
end

function keggle()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	local currentTime = player.CurrentTime()
	
	local startTime = currentTime - 0.21069
	
	for i=1,actionCount do
		local actionTime = script.actions[i].at
		if actionTime > startTime + 0.5 then
			break
		elseif actionTime >= currentTime - 0.22 and actionTime <= currentTime + 0.22 then
			return
		end
	end
	
	script.actions:add(Action.new(startTime, 0))
	script.actions:add(Action.new(startTime + 0.04218, 19))
	script.actions:add(Action.new(startTime + 0.08429, 25))
	script.actions:add(Action.new(startTime + 0.12646, 44))
	script.actions:add(Action.new(startTime + 0.16858, 50))
	script.actions:add(Action.new(startTime + 0.21069, 69))
	script.actions:add(Action.new(startTime + 0.42145, 0))	
	
	script:sort()
	script:commit()
end

function pulsingVibration()
	local script = ofs.Script(ofs.ActiveIdx())
	local filter = function(first,second) return second.at - first.at > 0.12 end
    local targetActions = common.getTargetActions(script,filter)
	if targetActions == nil or #targetActions == 0 then
		return
	end
	
	common.StartAddingActions()
	
	for idx, targetAction in ipairs(targetActions) do	
		local midAt = (targetAction.second.at + targetAction.first.at)/2
		local midAction = {at = midAt, pos = common.getPosAtTime(targetAction.first, targetAction.second, midAt)}
		
		local forceTop = true
		local fadeIn = true
		local fadeInVibrations = vibrate.getVibrationActions(targetAction.first, midAction, 0.03, 100, forceTop, false, fadeIn, not fadeIn, 100)
		if fadeInVibrations == nil then
			goto pulsingVibrationContinue
		end
		
		local fadeOutVibrations = vibrate.getVibrationActions(midAction, targetAction.second, 0.03, 100, forceTop, false, not fadeIn, fadeIn, 100)
		if fadeOutVibrations == nil then
			goto pulsingVibrationContinue
		end
		
		common.AddAction(targetAction.first, targetAction.second, midAction.at, midAction.pos)
		
		for k, newAction in ipairs(fadeInVibrations) do
			common.AddAction(targetAction.first, midAction, newAction.at, newAction.pos)
		end
		
		for k, newAction in ipairs(fadeOutVibrations) do
			common.AddAction(midAction, targetAction.second, newAction.at, newAction.pos)
		end
		
		::pulsingVibrationContinue::
	end
   
    common.commitNewActions(script)
end

return Preset