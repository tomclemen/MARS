# Source: https://www.littlemissdata.com/blog/simple-eda

library(readr)
library(dplyr)
library(skimr)
library(devtools)
library(visdat)
library(DataExplorer)
library(ggplot2)
ticks <- 8736
tree_no <- 2

col_t = cols_only("Tick"="i","DateTime"="c","DeadwoodMass"="d","HasLeaves"="l",
                  "Height"="d","Latitude"="d","LivingWoodMass"="d",
                  "Longitude"="d", "WoodMass"="d", "ID"=col_guess())
#df = read_csv2('Tree.csv', col_names = TRUE, col_types = col_t) # from readr
df = read_csv2('Tree_8736.csv', col_names = TRUE) # from readr

df$Time <- format(as.POSIXct(df$DateTime,format="%Y-%m-%dT%H:%M:%S"),"%H:%M:%S")
df$Date <- format(as.POSIXct(df$DateTime,format="%Y-%m-%dT%H:%M:%S"),"%Y:%m:%d")

df_select <- df %>% select("ID", "Tick", "Date","Time", "Latitude", "Longitude", "HasLeaves",
                        "Height", "DeadwoodMass", "LivingWoodMass", "WoodMass")

df_sort <- df_select[order(df_select$ID),]

tree <- df_sort[((tree_no-1)*ticks+2):((tree_no*ticks)+2),]

ggplot(as.data.frame(tree), aes(x=tree$Tick, y=Height)) +
  geom_point(size=1)


# head(df_sort,10)
# 
# dim(df_sort)
# 
# glimpse(df_sort) # from dplyr
# 
# summary(df_sort)
# 
# skim(df_sort) # from skimr
# 
# vis_miss(df_sort) # from visdat
# 
# vis_dat(df_sort) # from visdat
# 
# create_report(df_sort) # from DataExplorer