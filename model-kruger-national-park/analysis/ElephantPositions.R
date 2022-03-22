install.packages("tidyverse")
install.packages("magrittr")
install.packages("quantreg")

library(magrittr)
library(tidyverse)
library(quantreg)
library(sf)

getwd()

elephantFile <- read.csv("result_data/Elephant_150519_1426.csv")
elephantSelection <- elephantFile[c(0:1000),]
elephantSelection<-elephantSelection[!(elephantSelection$Latitude==0),]

ggplot(data = elephantSelection) +
  geom_point(mapping = aes(x = Longitude, y=Latitude))

ggplot(elephantSelection, aes(x = Longitude, y = Latitude)) +
  coord_quickmap() +
  geom_point()
  
locations_sf <- st_as_sf(elephantSelection, coords = c("Longitude", "Latitude"), crs = 4326)
ggplot(locations_sf, aes(x = geometry[1], y = geometry[2])) +
  coord_quickmap() +
  geom_point()

jpeg("result_images/AccelerateFrom30To50Test.jpg", width = 800, height = 300) 

ggplot(data = carCsvFile) +
  geom_line(mapping = aes(x = Step, y = Velocity)) +
  theme(text = element_text(size=20)) +
  geom_hline(yintercept=8.35, linetype="dotdash", color = "red") +
  geom_hline(yintercept=13.9, linetype="dotdash", color = "red") 


dev.off()