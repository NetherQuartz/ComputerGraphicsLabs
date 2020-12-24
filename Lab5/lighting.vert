#version 330

uniform mat4 PMatrix;
uniform mat4 MVMatrix;

attribute vec3 vPosition;
attribute vec3 vNormal;
attribute vec3 vColor;

uniform vec3 Ka, Kd, Ks;
uniform vec3 Ia, Il;
uniform float P;
uniform float K1, K2;
uniform vec3 LightPos, WatcherPos;

varying vec3 position;
varying vec3 normal;
varying vec3 color;

void main() {
    position = vPosition;
    normal = normalize(vNormal);
    color = vColor;
    gl_Position = PMatrix * MVMatrix * vec4(vPosition, 1.0);
}