all: build generate test

build:
	./docker_build.sh

generate:
	./generate_bindings.sh

test:
	./docker_test_bindings.sh
