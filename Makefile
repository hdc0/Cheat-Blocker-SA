CSC = csc

SOURCES = \
	CheatBlockerSA.cs \
	GtaVersions.cs \
	ListViewItemComparer.cs \
	LocalProcess.cs \
	ProcessSelectionForm.cs \
	Program.cs \
	WinApi.cs

cheat_blocker_sa.exe: $(SOURCES)
	$(CSC) $(CSFLAGS) /optimize+ /out:$@ /platform:x86 /target:winexe $(SOURCES)
