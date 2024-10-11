/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

use std::fmt;

#[derive(uniffi::Enum)]
pub enum Shape {
    Rectangle { s: Rectangle },
    Ellipse { s: Ellipse },
}

#[derive(Debug, thiserror::Error, uniffi::Error)]
pub enum ShapeError {
    #[error("Rectangle: {s}")]
    Rectangle { s: Rectangle },
    #[error("Ellipse: {s}")]
    Ellipse { s: Ellipse },
}

#[derive(uniffi::Record, Debug)]
pub struct Rectangle {
    width: f64,
    height: f64,
}

#[derive(uniffi::Record, Debug)]
pub struct Ellipse {
    x_radius: f64,
    y_radius: f64,
}

impl fmt::Display for Rectangle {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(f, "width: {}, height: {}", self.width, self.height)
    }
}

impl fmt::Display for Ellipse {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(f, "x_radius: {}, y_radius: {}", self.x_radius, self.y_radius)
    }
}

uniffi::setup_scaffolding!();
