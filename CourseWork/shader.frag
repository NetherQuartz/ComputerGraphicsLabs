#version 330

const vec3 Ka = vec3(0.5, 0.5, 0.5);
const vec3 Kd = vec3(1.0, 1.0, 1.0);
const vec3 Ia = vec3(0.6, 0.9, 0.8);
const vec3 Il1 = vec3(0.3, 0.5, 0.2);
const vec3 Il2 = vec3(0.2, 0.3, 0.3);
const float K1 = 0.5;
const float K2 = 0.2;

varying vec3 position;
varying vec3 normal;
varying vec3 color;

const float Pi = 3.1415926;

vec3 bound(vec3 v) {
    return min(max(v, 0.0), 1.0);
}

void main() {
    vec3 lightPos1 = vec3(1.0, 1.0, 1.0);
    vec3 i = Ia * Ka;
    float d = length(lightPos1 - position);
    vec3 L = normalize(lightPos1 - position);
    float LN = dot(L, normal);
    if (LN > 0) {
        i += Il1 * Kd * LN / (d * K1 + K2);
    }

    vec3 lightPos2 = vec3(-1.0, -1.0, -1.0);
    d = length(lightPos2 - position);
    L = normalize(lightPos2 - position);
    LN = dot(L, normal);
    if (LN > 0) {
        i += Il2 * Kd * LN / (d * K1 + K2);
    }
    gl_FragColor = vec4(bound(color * i), 1.0);
}