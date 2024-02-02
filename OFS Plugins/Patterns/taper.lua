local Taper = {}

function Taper.shrink()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	local currentTime = player.CurrentTime()
	
	local nextAction = nil
	local haveStartingAction = false
	for i=1,actionCount do
		nextAction = script.actions[i]
		haveStartingAction = haveStartingAction or (math.abs(nextAction.at - currentTime) < 0.001)
		if nextAction.at > currentTime + 0.001 and nextAction.at < currentTime + 0.261 then
			return
		end
		
		if nextAction.at > currentTime + 0.261 then
			break
		end
	end
	
	local newActions = {}
	
	if not haveStartingAction then
		table.insert(newActions, {at=currentTime, pos=0})
	end
	
	table.insert(newActions, {at=currentTime+0.04, pos=18})
	table.insert(newActions, {at=currentTime+0.08, pos=0})
	
	table.insert(newActions, {at=currentTime+0.115, pos=15})
	table.insert(newActions, {at=currentTime+0.150, pos=0})
	
	table.insert(newActions, {at=currentTime+0.180, pos=13})
	table.insert(newActions, {at=currentTime+0.210, pos=0})
	
	if nextAction.at > currentTime+0.260 then
		table.insert(newActions, {at=currentTime+0.235, pos=11})
		table.insert(newActions, {at=currentTime+0.260, pos=0})
	end
	
	if nextAction.at > currentTime+0.308 then
		table.insert(newActions, {at=currentTime+0.284, pos=10})
		table.insert(newActions, {at=currentTime+0.308, pos=0})
	end
	
	if nextAction.at > currentTime+0.354 then
		table.insert(newActions, {at=currentTime+0.331, pos=10})
		table.insert(newActions, {at=currentTime+0.354, pos=0})
	end
	
	if nextAction.at > currentTime+0.398 then
		table.insert(newActions, {at=currentTime+0.376, pos=9})
		table.insert(newActions, {at=currentTime+0.398, pos=0})
	end
	
	if nextAction.at > currentTime+0.440 then
		table.insert(newActions, {at=currentTime+0.419, pos=9})
		table.insert(newActions, {at=currentTime+0.440, pos=0})
	end
	
	local nextTop = 9
	local gap = 0.04
	currentTime = currentTime + 0.440
	while nextTop > 2 do
		if nextAction.at > currentTime+gap then
			table.insert(newActions, {at=currentTime+(gap/2), pos=nextTop})
			table.insert(newActions, {at=currentTime+gap, pos=0})
			currentTime = currentTime + gap
			nextTop = nextTop - 1
		else
			break
		end
	end
	
	for idx, newAction in ipairs(newActions) do
		script.actions:add(Action.new(newAction.at, newAction.pos))
	end
	
	script:sort()
	script:commit()
end

function Taper.growThenShrink()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	local currentTime = player.CurrentTime()
	
	for i=1,actionCount do
		local actionTime = script.actions[i].at
		if actionTime >= currentTime - 0.2 and actionTime <= currentTime + 0.2 then
			return
		end
	end
	
	script.actions:add(Action.new(currentTime - 0.195, 0))
	script.actions:add(Action.new(currentTime - 0.17, 11))
	script.actions:add(Action.new(currentTime - 0.145, 0))
	script.actions:add(Action.new(currentTime - 0.12, 20))
	script.actions:add(Action.new(currentTime - 0.095, 9))
	script.actions:add(Action.new(currentTime - 0.065, 30))
	script.actions:add(Action.new(currentTime - 0.035, 17))
	script.actions:add(Action.new(currentTime, 40))
	script.actions:add(Action.new(currentTime + 0.035, 17))
	script.actions:add(Action.new(currentTime + 0.065, 30))
	script.actions:add(Action.new(currentTime + 0.095, 9))
	script.actions:add(Action.new(currentTime + 0.12, 20))
	script.actions:add(Action.new(currentTime + 0.145, 0))	
	script.actions:add(Action.new(currentTime + 0.17, 11))
	script.actions:add(Action.new(currentTime + 0.195, 0))
	
	script:sort()
	script:commit()
end

return Taper