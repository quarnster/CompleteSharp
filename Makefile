SCRIPT=import json; print json.load(open('package.json'))['packages'][0]['platforms']['*'][0]['version']
VERSION=$(shell python -c "$(SCRIPT)")

all: clean CompleteSharp.exe release

CompleteSharp.exe: CompleteSharp.cs
	mcs -platform:x86 $<

clean:
	rm -rf release

release:
	mkdir release
	cp -r sublimecompletioncommon release
	find . -maxdepth 1 -type f -exec cp {} release \;
	find release -name ".git*" | xargs rm -rf
	find release -name "*.pyc" -exec rm {} \;
	find release -name "unittest*" -exec rm {} \;
	rm release/Makefile
	cd release && zip -r CompleteSharp-$(VERSION).sublime-package *
