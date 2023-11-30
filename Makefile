all: build generate test

build:
	./build.sh

generate:
	./generate_bindings.sh

test:
	./test_bindings.sh
